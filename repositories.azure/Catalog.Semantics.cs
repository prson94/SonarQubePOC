using d360.core.entities;
using d360.core.enums;
using Dapper;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Newtonsoft.Json.Linq;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Catalog
	{

		public async Task<RepositoryResponse<PagedApiBaseViewModel<GetSemantic>>> ReadSemanticTypesAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			RepositoryResponse<PagedApiBaseViewModel<GetSemantic>> response = new(null, 200, true);

			List<FilterColumnOption> filterOptions = [
				new FilterColumnOption("uid", "S.Uid", SqlFieldType.Guid),
				new FilterColumnOption("name", "S.Name", SqlFieldType.Text),
				new FilterColumnOption("description", "S.Description", SqlFieldType.Text),
				new FilterColumnOption("qualifier", "S.Qualifier", SqlFieldType.Text),
				new FilterColumnOption("source", SemanticSource.BuiltIn.GetSqlCaseFilterStatement("S.[Source]"), SqlFieldType.Text),
				new FilterColumnOption("status", SemanticStatus.Draft.GetSqlCaseFilterStatement("S.Status"), SqlFieldType.Text),
				new FilterColumnOption("baseType", SemanticBaseType.Boolean.GetSqlCaseFilterStatement("S.BaseType"), SqlFieldType.Text),
				new FilterColumnOption("matchType", SemanticMatchType.Advanced.GetSqlCaseFilterStatement("S.MatchType"), SqlFieldType.Text),
				new FilterColumnOption("effectiveDate", "S.EffectiveDate", SqlFieldType.DateTime),
				new FilterColumnOption("threshold", "S.Threshold", SqlFieldType.Number),
				new FilterColumnOption("priority", "S.Priority", SqlFieldType.Number),
				new FilterColumnOption("createdBy", "S.CreatedBy", SqlFieldType.Text),
				new FilterColumnOption("createdOn", "CAST(S.CreatedOn as DATE)", SqlFieldType.DateTime),
				new FilterColumnOption("updatedBy", "S.UpdatedBy", SqlFieldType.Text),
				new FilterColumnOption("updatedOn", "CAST(S.UpdatedOn as DATE)", SqlFieldType.DateTime)
			];

			// Parse and get back any advanced filters, and load dbArguments and where clauses.
			var advancedFilters = queryParams.ParseODataFilters();//.ParseAdvancedFilters();
			var (dbArgs, wheres) = advancedFilters.ConvertToSqlFilters(filterOptions);

			// Parse page size and offset and load into arguments for SQL.
			int pageNumber = queryParams.CheckForPageNumber();
			int pageSize = queryParams.CheckForPageSize();
			dbArgs.LoadOffsetDatabaseParameter(pageNumber, pageSize);
			dbArgs.LoadPageSizeDatabaseParameter(pageSize);

			string parameterName = "_simplefilter";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var simpleFilter = queryParams.ReadQueryParameterValue(parameterName);
				if (!string.IsNullOrEmpty(simpleFilter))
				{
					simpleFilter = simpleFilter.Replace("*", "%");
					var possibleEnumStringValue = simpleFilter.Replace("%", "");

					dbArgs.Add("@simpleFilter", simpleFilter);

					var simpleFilters = new List<string>
					{
						$"S.Name like @simpleFilter",
						$"S.Description like @simpleFilter",
						$"S.Qualifier like @simpleFilter"
					};

					if (Enum.TryParse(possibleEnumStringValue, out SemanticSource source))
					{
						simpleFilters.Add($"S.[Source] = {(int)source}");
					}

					if (Enum.TryParse(possibleEnumStringValue, out SemanticStatus status))
					{
						simpleFilters.Add($"S.Status = {(int)status}");
					}

					simpleFilters.Add($"S.Threshold like @simpleFilter");
					simpleFilters.Add($"S.Priority like @simpleFilter");

					if (Enum.TryParse(possibleEnumStringValue, out SemanticBaseType baseType))
					{
						simpleFilters.Add($"S.BaseType = {(int)baseType}");
					}

					simpleFilters.Add($"S.EffectiveDate like @simpleFilter");

					wheres.Add($"({string.Join(" or ", simpleFilters)})");
				}
			}

			parameterName = "_includedisabled";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{ 
				string includeDisabledStringValue = queryParams.ReadQueryParameterValue(parameterName);
				if (bool.TryParse(includeDisabledStringValue, out bool includeDisabled))
				{
					if (!includeDisabled)
					{
						wheres.Add("S.EffectiveDate = S.UpdatedOn");
					}
				}
			}

			DateTime asOfEffectiveDate = DateTime.UtcNow;
			parameterName = "asofeffectivedate";
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				string asOfDateStringValue = queryParams.ReadQueryParameterValue(parameterName);
				if (!DateTime.TryParse(asOfDateStringValue, out asOfEffectiveDate))
				{
					asOfEffectiveDate = DateTime.UtcNow;
				}
			}
			dbArgs.Add("@asOfEffectiveDate", asOfEffectiveDate);

			string sortColumn = queryParams.CheckForSortColumn(
				[
					new SortColumnOption("baseType", "S.BaseType"),
					new SortColumnOption("description", "S.Description"),
					new SortColumnOption("effectiveDate", "S.EffectiveDate"),
					new SortColumnOption("headerRegExpConfidence", "S.HeaderFilterConfidence"),
					new SortColumnOption("matchType", "S.MatchType"),
					new SortColumnOption("maximum", "S.Maximum"),
					new SortColumnOption("minimum", "S.Minimum"),
					new SortColumnOption("minSamples", "S.MinimumSamples"),
					new SortColumnOption("minMaxPresent", "S.MinMaxPresent"),
					new SortColumnOption("name", "S.Name"),
					new SortColumnOption("priority", "S.Priority"),
					new SortColumnOption("qualifier", "S.Qualifier"),
					new SortColumnOption("status", "StatusString"),
					new SortColumnOption("threshold", "S.Threshold"),
					new SortColumnOption("isDisabled", "case when S.EffectiveDate < S.UpdatedOn then 1 else 0 end")
				], "S.Qualifier");
			string sortDirection = queryParams.CheckForSortDirection();

			string statusSql = "";
			if (sortColumn == "StatusString")
			{
				StringBuilder statusBuilder = new StringBuilder(", CASE");
				foreach (int i in Enum.GetValues(typeof(SemanticStatus)))
				{
					statusBuilder.Append($@" WHEN status = {i} then '{Enum.GetName(typeof(SemanticStatus), i)}'");
				}
				statusBuilder.Append(" ELSE '' END as StatusString");

				statusSql = statusBuilder.ToString() + " " + sortDirection;
			}

			string whereSql = "where " + string.Join(" and ", wheres);
			string orderBySql = $"{sortColumn} {sortDirection}";
			
			var tableQuery = $"select ROW_NUMBER() OVER(PARTITION BY Qualifier ORDER BY EffectiveDate desc ) AS RowNum, * {statusSql} from Semantic where EffectiveDate <= @asOfEffectiveDate";
			var sql = $@"
select	count(1) as [Count] from ({tableQuery}) S {whereSql}
select	S.*, 
		case
			when exists(select 1 from AssetDataProfile where Qualifier = S.Qualifier) then 1
			else 0
		end as hasQualifiedAssets,
		(
		select	FORMAT(EffectiveDate, 'yyyy-MM-ddThh.mm.ss') as effectiveDate,
				FORMAT(UpdatedOn, 'yyyy-MM-ddThh.mm.ss') as updatedOn
		from	Semantic
		where	Qualifier = S.Qualifier
		for json path
		) as dates,
		c.uid as createdByUid, 
		c.FirstName + ' ' + c.LastName as createdByFullName, 
		u.uid as updatedByUid, 
		u.FirstName + ' ' + u.LastName as updatedByFullName 
from	({tableQuery}) S 
		left join reporting.Global_Resource c on c.ResourceID = S.CreatedBy
		left join reporting.Global_Resource u on u.ResourceID = S.UpdatedBy
{whereSql} 
order by {orderBySql} 
OFFSET @offset ROWS FETCH NEXT @size ROWS ONLY";

			using (var connection = ConnectionProvider.Connect(true))
			{
				var query = await connection.QueryMultipleAsync(sql, dbArgs);
				int total = await query.ReadSingleAsync<int>();
				var items = (await query.ReadAsync<Semantic>()).AsList();

				var models = items.Select(o => o.ToGetModel()).ToList();	
				response.Data = new PagedApiBaseViewModel<GetSemantic>
				{
					items = models,
					pageNum = pageNumber,
					pageSize = pageSize,
					total = total
				};
			}

			return response;
		}

	}
}
