using AngleSharp.Text;
using d360.core;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
	public partial class AssetsController : BaseV2ApiController
	{
		#region Supporting classes for the endpoint

		internal class PredValue
		{
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class PropertyDefinition
		{
			[JsonProperty("name")]
			public string Name { get; set; }

			[JsonProperty("label")]
			public string Label { get; set; }

			[JsonProperty("type")]
			public string Type { get; set; }

			[JsonProperty("options")]
			public List<string> Options { get; set; }
		}

		public class CatalogAssetsResponseModel
		{
			[JsonProperty("pageNum")]
			public int PageNum { get; set; }

			[JsonProperty("pageSize")]
			public int PageSize { get; set; }

			[JsonProperty("total")]
			public int Total { get; set; }

			[JsonProperty("items")]
			public IEnumerable<dynamic> Items { get; set; }

			[JsonProperty("definition")]
			public ICollection<PropertyDefinition> Definition { get; set; }
		}

		public class CatalogColumn
		{
			public string Column { get; set; }

			public string JoinStatement { get; set; }

			public string DataStatement { get; set; }

			public string ApiName { get; set; }

			public int Position { get; set; }

			public CatalogColumnType CatalogColumnType { get; set; }

		}

		public enum CatalogColumnType
		{
			SystemField, Predicate
		}

		#endregion

		/// <summary>
		/// Retrieves assets across types that are categorized using special relationships.
		/// </summary>
		/// <remarks>
		///Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example: `city eq 'Redmond'`.
		///*  For comparison operators on Text fields you can use eq (equal), neq (not equal), ct (contains), nct (not contains)
		///*  For comparison operators on List fields you can use in (in) and nin (not in)
		///     
		///Example :
		///     
		///- **Text Comparison Operators**
		///	- Equals operator - {fieldname} eq Data
		///	- Not equals operator - {fieldname} neq Data
		///	- Contains operator - {fieldname} ct Data
		///	- Not Contains operator - {fieldname} nct Data
		///- **List Comparison Operators**
		///	- In operator - {fieldname} in Data
		///	- In operator - {fieldname} in Data1,Data2
		///	- Not In operator - {fieldname} nin Data
		///	- Not In operator - {fieldname} nin Data1,Data2
		///
		///Sorting is done using _order parameter and sort expressions are specified using operator and property name. For example: `desc(displayValue)`.
		///
		///Examples using Ascending Order:
		///- asc(path)
		///- asc(displayValue)
		///
		///Examples using Descending Order:
		///- desc(path)
		///- desc(displayValue)
		/// </remarks>
		[
			HttpGet,
			Route(""),
			SwaggerResponse(HttpStatusCode.OK, "", typeof(CatalogAssetsResponseModel)),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			SwaggerParameter("_includeDefinition", "An option to include the definition of the fields, including lookup options for any list fields.", DataType = "boolean", ParameterType = "query", Required = false),
			SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 250.", DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
			SwaggerParameter("_order", "Use this field to set the direction and proeprty to sort by. The syntax is: asc(propertyName) or desc(propertyName). By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
			SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false)
		]
		public async Task<IHttpActionResult> GetCatalogAssetsAsync(CancellationToken cancellationToken)
		{
			var queryParams = Request.GetQueryNameValuePairs();
			string sql = "";

			// Get relevant predicates.
			sql = "select p.Id, p.Name from [Predicate] p where p.[Type] = 5 and exists (select 1 from IntersectType where PredicateId = p.Id)";
			var predicates = Company.Query<PredValue>(sql).ToList();

			#region Add columns

			var columns = new List<CatalogColumn> {
				new CatalogColumn { ApiName = "uid", Column = "a.Uid", Position = 1 },
				new CatalogColumn { ApiName = "displayValue", Column = "adv.DisplayValue", Position = 2 }
			};

			predicates.ForEach(p => {
				columns.Add(new CatalogColumn
				{
					ApiName = p.Name,
					Column = $@"P{p.Id}.val as [{p.Name}]",
					DataStatement = $"outer apply (select string_agg(SubjectDisplayValue, '; ') from dbo.CatalogBrowseSubject where ObjectAssetID = res.objectassetid and PredicateId = {p.Id})P{p.Id}(val)",
					Position = columns.Max(c => c.Position) + 1,
					JoinStatement = $"left join dbo.CatalogBrowseSubject T{p.Id} on T{p.Id}.ObjectAssetID = S.ObjectAssetID and T{p.Id}.PredicateId = {p.Id}",
					CatalogColumnType = CatalogColumnType.Predicate
				});
			});

			// Add path as the last column.
			columns.Add(new CatalogColumn { ApiName = "path", Column = "p.DisplayPath", Position = columns.Max(c => c.Position) + 1 });

			#endregion

			#region Filtering logic

			var wheres = new List<string> {
				"exists(select 1 from PredicateIntersect where PredicateType = 5 and ObjectAssetID = a.Id)"
			};
			var dbArgs = new DynamicParameters();
			if (queryParams.Any(q => q.Key == "_filter"))
			{
				var rawFilters = queryParams.Where(q => q.Key == "_filter").Select(s => s.Value).ToList();
				int parameterIndex = 1;
				rawFilters.ForEach(rawSort => {
					var filterMatch = Regex.Match(rawSort, @"^([\w\s\-\/\:]+)\s(ct|eq|in|nct|neq|nin)\s([\w\-\/\:\,\*]+)$");
					if (filterMatch.Success && filterMatch.Groups.Count == 4)
					{
						var filterProperty = filterMatch.Groups[1].Value;
						var filterOperation = filterMatch.Groups[2].Value;
						var filterValue = filterMatch.Groups[3].Value;
						var column = columns.FirstOrDefault(c => c.ApiName == filterProperty);
						if (column != null)
						{
							var predicate = predicates.FirstOrDefault(p => p.Name == filterProperty);
							if (predicate != null)
							{
								// Treat as a list: IN, NOT IN

								var filterText = $"v.DisplayValue {(filterOperation == "in" ? "in" : "not in")} (";
								var filterParamIds = new List<string>();
								if (filterValue.Contains(","))
								{
									var filterValues = filterValue.SplitCommas().ToList();
									int i = 1;
									filterValues.ForEach(fV =>
									{
										var paramId = $"p{predicate.Id}_{i}";
										dbArgs.Add(paramId, fV);
										filterParamIds.Add(paramId);
										i++;
									});
								}
								else
								{
									var paramId = $"p{predicate.Id}_1";
									dbArgs.Add(paramId, filterValue);
									filterParamIds.Add(paramId);
								}
								filterText += string.Join(",", filterParamIds.Select(o => $"@{o}")) + ")";
								wheres.Add($"exists(select 1 from PredicateIntersect i inner join #v v on v.AssetId = i.SubjectAssetId and i.PredicateType = 5 and i.PredicateId = {predicate.Id} and ObjectAssetID = a.Id and {filterText})");
							}
							else
							{
								// Treat as standard text value.
								var operation = "";
								bool shouldInclude = true;
								switch (filterOperation)
								{
									case "ct":
										operation = "like";
										if (filterValue.StartsWith("*"))
										{
											dbArgs.Add($"p{parameterIndex}", $"%{filterValue.Replace("*", "")}");
										}
										else if (filterValue.EndsWith("*"))
										{
											dbArgs.Add($"p{parameterIndex}", $"{filterValue.Replace("*", "")}%");
										}
										else
										{
											dbArgs.Add($"p{parameterIndex}", $"%{filterValue}%");
										}

										break;
									case "eq":
										operation = "=";
										dbArgs.Add($"p{parameterIndex}", filterValue);
										break;
									case "nct":
										operation = "not like";
										dbArgs.Add($"p{parameterIndex}", filterValue);
										break;
									case "neq":
										operation = "<>";
										dbArgs.Add($"p{parameterIndex}", filterValue);
										break;
									default:
										shouldInclude = false;
										break;
								}
								if (shouldInclude)
								{
									wheres.Add($"{column.Column} {operation} p{parameterIndex}");
								}
							}
						}
					}
					parameterIndex++;
				});
			}

			#endregion

			#region Sort logic

			var parsedSorts = new Dictionary<string, bool>();
			if (queryParams.Any(q => q.Key == "_order"))
			{
				var rawSorts = queryParams.Where(q => q.Key == "_order").Select(s => s.Value).ToList();
				rawSorts.ForEach(rawSort => {
					var sortMatch = Regex.Match(rawSort, @"^(asc|desc)\(([\w\d\s\-\/\:]+)\)$");
					if (sortMatch.Success && sortMatch.Groups.Count == 3)
					{
						var sortProperty = sortMatch.Groups[2].Value.Trim();
						var sortDirection = (sortMatch.Groups[1].Value == "asc");
						if (parsedSorts.ContainsKey(sortProperty))
						{
							parsedSorts[sortProperty] = sortDirection;
						}
						else
						{
							parsedSorts.Add(sortProperty, sortDirection);
						}
					}
				});
			}
			if (parsedSorts.Count == 0)
			{
				parsedSorts.Add("displayValue", true); //displayValue, based on column Position
			}

			var sorts = new List<string>();
			foreach (var key in parsedSorts.Keys)
			{
				var sortDirection = (parsedSorts[key] ? "asc" : "desc");
				var column = columns.FirstOrDefault(c => c.ApiName == key);
				if (column != null)
				{
					sorts.Add($"{column.Position} {sortDirection}");
				}
			}

			if (sorts.Count == 0)
			{
				sorts.Add($"2 asc");
			}

			#endregion

			bool includeDefinition = false;
			if (queryParams.Any(q => q.Key == "_includeDefinition"))
			{
				var rawIncludeDefinition = queryParams.Where(q => q.Key == "_includeDefinition").Select(s => s.Value).First().ToLower();
				includeDefinition = (rawIncludeDefinition == "true");
			}

			#region Offset logic

			int pageSize = 10;
			if (queryParams.Any(q => q.Key == "_pageSize"))
			{
				var _pageSize = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value;
				bool pageSizeValid = true;

				if (_pageSize.Length > 10)
				{
					pageSizeValid = false;
				}

				if (int.TryParse(_pageSize, out pageSize))
				{
					int maxRows = 250;

					if (pageSize <= 0)
					{
						pageSizeValid = false;
					}

					if (pageSize > maxRows)
					{
						pageSize = maxRows;
					}
				}
				else
				{
					pageSizeValid = false;
				}

				if (!pageSizeValid)
				{
					pageSize = 10;
				}
			}

			int pageNum = 1;
			if (queryParams.Any(q => q.Key == "_pageNum"))
			{
				var _pageNum = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value;
				bool pageNumValid = true;

				if (_pageNum.Length > 10)
				{
					pageNumValid = false;
				}

				if (int.TryParse(_pageNum, out pageNum))
				{
					if (pageNum <= 0)
					{
						pageNumValid = false;
					}
				}
				else
				{
					pageNumValid = false;
				}

				if (!pageNumValid)
				{
					pageNum = 1;
				}
			}

			var offset = $"offset {(pageNum - 1) * pageSize} rows fetch next {pageSize} rows only";

			#endregion

			#region Build Sql, including the Count, the Lookup, and the Resultset

			sql = $@"
drop table if exists #v; 
create table #v (PredicateId int, AssetId bigint, DisplayValue nvarchar(500));
insert into #v
	select	distinct
			p.Id,
			a.Id as AssetId,
			d.DisplayValue
	from	[Predicate] p
			inner join IntersectType t on t.PredicateId = p.Id and p.[Type] = 5
			inner join Asset a on a.AssetTypeId = t.SubjectAssetTypeId
			inner join AssetDisplayValue d on d.AssetId = a.Id;";

			if (includeDefinition)
			{
				sql += "select PredicateId as Id, DisplayValue as Name from #v;";
			}

			if (wheres.Count > 1)
			{
				sql += $@"
select	count(1) as [Total] 
from	Asset a 
		inner join AssetPath p on p.Id = A.Id
		inner join AssetDisplayValue v on v.AssetId = a.Id 
where	{string.Join(" and ", wheres)};";
			}
			else
			{
				sql += $"select count(1) as [Total] from Asset a where {string.Join(" and ", wheres)};";
			}

			sql += $@"select	{string.Join(", ", columns.Select(c => $"{c.Column} as [{c.ApiName.CleanForSql()}]"))}
from	Asset a
		inner join AssetPath p on p.Id = A.Id
		inner join AssetDisplayValue v on v.AssetId = a.Id
where	{string.Join(" and ", wheres)}
order by {string.Join(", ", sorts)}
{offset};";

			#endregion

			//new sql logic
			string sortColumn = "S.ObjectDisplayValue";
			string sortDir = "asc";

			string definitionSql = "";
			if (includeDefinition)
			{
				definitionSql = $@"
					create table #v (PredicateId int, AssetId bigint, DisplayValue nvarchar(500));
					insert into #v
						select	distinct
								p.Id,
								a.Id as AssetId,
								d.DisplayValue
						from	[Predicate] p
								inner join IntersectType t on t.PredicateId = p.Id and p.[Type] = 5
								inner join Asset a on a.AssetTypeId = t.SubjectAssetTypeId
								inner join AssetDisplayValue d on d.AssetId = a.Id;

					select PredicateId as Id, DisplayValue as Name from #v;
					";
			}

			string baseSQL = $@"
				select {{0}}
				from
				dbo.CatalogBrowseObject S
				{string.Join(Environment.NewLine, columns.Where(x=> !string.IsNullOrEmpty(x.JoinStatement)).Select(x=> x.JoinStatement))}";


			string countSql = $@"
				;with cte as (
				{string.Format(baseSQL, "count(1) as cnt")}
				group by {sortColumn}
				)
				select COUNT(1) from cte;";

			string resultsSql = $@"
				declare @results table (objectassetid int);

				insert into @results
				{string.Format(baseSQL, "MAX(S.ObjectAssetId) AS ObjectAssetId")}
				group by {sortColumn}
				order by {sortColumn} {sortDir}
				{offset}";

			string finalSql = $@"
				{definitionSql}

				{countSql}

				{resultsSql}

				select {string.Join(","+ Environment.NewLine, columns.Select(x=> x.Column))}
				from @results res
				inner join AssetDisplayValue adv on adv.AssetID = res.objectassetid
				inner join asset a on a.ID = res.objectassetid
				inner join AssetPath p on p.Id = A.Id
				{string.Join(Environment.NewLine, columns.Where(x=> x.CatalogColumnType == CatalogColumnType.Predicate).Select(x => x.DataStatement))}
				";

			var results = await Company.QueryMultipleAsync(finalSql, dbArgs);

			List<PropertyDefinition> properties = null;
			if (includeDefinition)
			{
				var optionsList = results.Read<PredValue>().ToList();

				properties = new List<PropertyDefinition> {
					new PropertyDefinition { Label = "Name", Name = "displayValue", Type = "Text" },
					new PropertyDefinition { Label = "Path", Name = "path", Type = "Text" }
				};

				predicates.ForEach(p =>
				{
					var property = new PropertyDefinition { Label = p.Name, Name = p.Name, Type = "List" };
					property.Options = optionsList
						.Where(v => v.Id == p.Id)
						.Select(o => o.Name)
						.OrderBy(o => o)
						.ToList();
					properties.Add(property);
				});
			}
			int total = results.Read<int>().First();
			var assets = results.Read<dynamic>();

			var response = Request.CreateResponse(
				HttpStatusCode.OK, new CatalogAssetsResponseModel
				{
					PageSize = pageSize,
					PageNum = pageNum,
					Total = total,
					Items = assets,
					Definition = properties
				});
			return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
		}
	}
}