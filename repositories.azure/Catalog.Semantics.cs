using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using Dapper;
using repositories.azure.extensions;

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

			string whereSql = "";

			wheres.Add($"S.RowNum = 1");

			whereSql = "where " + string.Join(" and ", wheres);

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

		public async Task<HttpStatusCode> DeleteSemanticAsync(string qualifier)
		{
			var qualifiers = new List<string> { qualifier };
			List<Semantic> deletes = findLatestExistingSemantics(qualifiers, 1);

			if (deletes.Any((s) => s.Source == SemanticSource.BuiltIn))
			{
				throw new GenericException(
					HttpStatusCode.Conflict,
					"Built in Semantic.",
					"Built-In semantic types cannot be deleted.");
			}

			var query = @"select  count(1) from    AssetDataProfile P 
																	cross apply (
																		select  max(EffectiveDate) as EffectiveDate
																		from    Semantic 
																		where   Qualifier = @qualifier 
																				and EffectiveDate <= P.ProfileSetDate
																	) S
															where   P.TypeQualifier = @qualifier";

			using (var connection = ConnectionProvider.Connect())
			{

				var anyProfilesQuery = await connection.ExecuteScalarAsync<int>(query, new { qualifier });

				if (anyProfilesQuery > 0)
				{
					throw new GenericException(
						HttpStatusCode.Conflict,
						"Profiles match the semantic.",
						"You may not remove this semantic since one or more asset data profiles match this semantic.");
				}

				connection.Execute("delete Semantic where Qualifier = @qualifier", new { qualifier });
			}
			return HttpStatusCode.OK;
		}

		public async Task<List<GetSemantic>> PostSemanticsAsync(List<PostSemantic> semantics)
		{
			var repoModels = semantics.Select(s => s.ToRepositoryModel(CurrentUserId)).ToList();
			var transactionId = generateTransactionId();
			repoModels.ForEach(s =>
			{
				s.Validate();
				s.TransactionId = transactionId;
			});
			var qualifiers = repoModels.Select(m => m.Qualifier).ToList();
			string selQualifierQuery = @"SELECT DISTINCT Qualifier FROM Semantic WHERE Qualifier IN @qualifiers";

			using (var connection = ConnectionProvider.Connect())
			{
				var matches = connection.Query<string>(selQualifierQuery, new { qualifiers }).ToList();

				if (matches.Count > 0)
				{
					throw new GenericException(HttpStatusCode.Conflict,
						"Potential conflicting semantics.",
						$"The following qualifiers already exist in your environment: {string.Join("; ", matches)}.");
				}

				connection.Open();

				connection.Execute(insertSemanticQuery(), repoModels);

				addToChangeLog(transactionId, "C");

				string selQuery = @" SELECT * FROM Semantic WHERE Qualifier IN @qualifiers";

				var createdSemantics = connection.Query<Semantic>(
					selQuery,
					new { qualifiers }
				).ToList();

				var globalresource = await GetSemanticCreatorUpdator(transactionId);

				var getModels = (
							  from s in createdSemantics
							  join c in globalresource on s.CreatedBy equals c.ResourceID
							  join u in globalresource on s.UpdatedBy equals u.ResourceID
							  select s.ToGetModel(c, u)
							  ).ToList();

				return getModels;
			}
		}

		public async Task<List<GetSemantic>> PutSemanticsAsync(List<PutSemantic> semantics)
		{
			var qualifiers = semantics.Select(o => o.Qualifier).ToList();
			var existingSemantics = findLatestExistingSemantics(qualifiers, semantics.Count);

			#region Built-in semantic checking

			var nonUpdatedableSemantics = new List<string>();
			existingSemantics.ForEach(e =>
			{
				if (e.Source == SemanticSource.BuiltIn || e.EffectiveDate != e.UpdatedOn)
				{
					nonUpdatedableSemantics.Add(e.Qualifier);
				}
			});

			if (nonUpdatedableSemantics.Count > 0)
			{
				throw new GenericException(
					HttpStatusCode.Conflict,
					"Semantics may not be updated.",
					$"The following semantics are Built-in or disabled and may not be updated: {string.Join("; ", nonUpdatedableSemantics)}."
					);
			}

			#endregion

			var repoModels = (
				from u in semantics
				join e in existingSemantics on u.Qualifier.ToLower() equals e.Qualifier.ToLower()
				select u.ToRepositoryModel(e, CurrentUserId)
				).ToList();

			var transactionId = generateTransactionId();
			repoModels.ForEach(s =>
			{
				s.Validate();
				s.TransactionId = transactionId;
			});

			using (var connection = ConnectionProvider.Connect())
			{
				connection.Open();
				connection.Execute(insertSemanticQuery(), repoModels);
			}

			addToChangeLog(transactionId, "U");

			var globalresource = await GetSemanticCreatorUpdator(transactionId);

			var getModels = (
							from s in repoModels
							join c in globalresource on s.CreatedBy equals c.ResourceID
							join u in globalresource on s.UpdatedBy equals u.ResourceID
							select s.ToGetModel(c, u)
							).ToList();

			return getModels;
		}

		public async Task<List<GetSemantic>> PatchSemanticsAsync(List<PatchSemantic> semantics)
		{
			var qualifiers = semantics.Select(o => o.Qualifier).ToList();
			var existingSemantics = findLatestExistingSemantics(qualifiers, semantics.Count);

			#region Built-in semantic checking

			var nonUpdatedableSemantics = new List<string>();
			existingSemantics.ForEach(e =>
			{
				var patchModel = semantics.SingleOrDefault(o => o.Qualifier == e.Qualifier);
				if (patchModel != null)
				{
					if (patchModel.BaseType.HasValue
							|| patchModel.HeaderFilter != null
							|| patchModel.HeaderFilterConfidence.HasValue
							|| patchModel.InvalidValuesStructured != null
							|| patchModel.JsonPayloadStructured != null
							|| patchModel.MatchType.HasValue
							|| patchModel.Maximum.HasValue
							|| patchModel.Minimum.HasValue
							|| patchModel.MinMaxPresent.HasValue
							|| patchModel.Priority.HasValue
							|| !string.IsNullOrEmpty(patchModel.RegularExpression)
							|| patchModel.Status.HasValue
							|| patchModel.Threshold.HasValue
							|| patchModel.IsDisabled.HasValue && patchModel.IsDisabled.Value == true
							|| patchModel.ValidLocalesStructured != null
							|| patchModel.ValidValuesStructured != null)
					{
						if (e.Source == SemanticSource.BuiltIn)
						{
							nonUpdatedableSemantics.Add(e.Qualifier);
						}
						if (patchModel.IsDisabled.HasValue && patchModel.IsDisabled.Value)
						{
							if (patchModel.Description?.Length > 0 || patchModel.Name?.Length > 0)
							{
								throw new GenericException(
									HttpStatusCode.Conflict,
									"Semantics cannot be updated while also being disabled.",
									$"Semantics cannot be updated while also being disabled. Please specify provide the qualifer and isDisabled values when disabling"
									);
							}
						}
					}
				}
			});

			if (nonUpdatedableSemantics.Count > 0)
			{
				throw new GenericException(
					HttpStatusCode.Conflict,
					"Semantics may not be updated.",
					$"The following semantics are Built-in and may not have the specified properties updated: {string.Join("; ", nonUpdatedableSemantics)}."
					);
			}

			#endregion

			var repoModels = (
				from u in semantics
				join e in existingSemantics on u.Qualifier.ToLower() equals e.Qualifier.ToLower()
				select u.ToRepositoryModel(e, CurrentUserId)
				).ToList();

			var transactionId = generateTransactionId();
			repoModels.ForEach(s =>
			{
				s.Validate();
				s.TransactionId = transactionId;
			});

			if (repoModels.Count > 0)
			{
				using (var connection = ConnectionProvider.Connect())
				{
					connection.Open();
					connection.Execute(insertSemanticQuery(), repoModels);
				}
			}

			addToChangeLog(transactionId, "U");

			var globalresource = await GetSemanticCreatorUpdator(transactionId);

			var getModels = (
							from s in repoModels
							join c in globalresource on s.CreatedBy equals c.ResourceID
							join u in globalresource on s.UpdatedBy equals u.ResourceID
							select s.ToGetModel(c, u)
							).ToList();

			return getModels;
		}

		public async Task<List<GetSemantic>> GetSemanticVersionsByQualifierAsync(string qualifier, IEnumerable<KeyValuePair<string, string>> queryParams, CancellationToken? cancellationToken = null)
		{
			if (cancellationToken == null)
			{
				cancellationToken = CancellationToken.None;
			}

			var qualifiers = new List<string> { qualifier };
			findLatestExistingSemantics(qualifiers, 1);

			var dbArgs = new DynamicParameters();
			dbArgs.Add("@qualifier", qualifier);

			var order = "EffectiveDate";
			var direction = "desc";
			var statusSQL = "";

			if (queryParams.ToList().Any(x => x.Key.ToLower() == "_order"))
			{
				order = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value;

				if (complexNonSortableFields.Contains(order))
				{
					throw new GenericException(HttpStatusCode.BadRequest, "You have provided a complex non-sortable field as an order parameter.");
				}

				if (!orderFields.ContainsKey(order))
				{
					throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid field as an order parameter.");
				}

				if (order.Equals("status", StringComparison.InvariantCultureIgnoreCase))
				{
					statusSQL = buildStatusOrderSQL();
				}

				order = orderFields[order]; // Get the appropriate column name.                
			}

			if (queryParams.ToList().Any(x => x.Key.ToLower() == "_direction"))
			{
				direction = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value;

				if (direction != "asc" && direction != "desc")
				{
					throw new GenericException(HttpStatusCode.BadRequest, "You have provided an invalid direction parameter.");
				}
			}

			var sql = $@"
						drop table if exists #tempsemantic;

						select * 
						into #tempsemantic
						from (select * {statusSQL} 
						from Semantic 
						where Qualifier = @qualifier) S;

						select * from #tempsemantic S
						order by {order} {direction};

						with rs_data as
						(select distinct * 
						from (
							select CreatedBy ResourceID from #tempsemantic
							union all
							select UpdatedBy ResourceID from #tempsemantic
							 ) a
						)
						select gr.*
						from rs_data s
						inner join [reporting].[Global_Resource] gr on gr.ResourceID = s.ResourceID
						";

			using (var connection = ConnectionProvider.Connect())
			{
				var command = new CommandDefinition(
					commandText: sql,
					parameters: dbArgs,
					cancellationToken: cancellationToken.Value,
					commandTimeout: CommandTimeout
				);

				var gridReader = await connection.QueryMultipleAsync(command);

				var repoModels = gridReader.Read<Semantic>().ToList();
				var globalresource = gridReader.Read<GlobalReportingResource>().ToList();

				var getModels = (
								from s in repoModels
								join c in globalresource on s.CreatedBy equals c.ResourceID
								join u in globalresource on s.UpdatedBy equals u.ResourceID
								select s.ToGetModel(c, u)
								).ToList();

				return getModels;
			}
		}

		public List<Semantic> GetSemanticsByQualifiers(List<string> qualifiers)
		{
			return findLatestExistingSemantics(qualifiers, 0);
		}

		public async Task<IEnumerable<dynamic>> GetPossibleCreators()
		{
			var sql = @"select distinct Semantic.CreatedBy as Id, globalResource.FirstName + ' ' + globalResource.LastName as Name
						from dbo.Semantic
							join reporting.Global_Resource globalResource on globalResource.ResourceID = Semantic.CreatedBy
						order by globalResource.FirstName + ' ' + globalResource.LastName";

			using (var connection = ConnectionProvider.Connect(true))
			{
				var results = await connection.QueryAsync(sql);
				return results;
			}
		}

		public async Task<IEnumerable<dynamic>> GetPossibleRedactors()
		{
			var sql = @"select distinct Semantic.UpdatedBy as Id, globalResource.FirstName + ' ' + globalResource.LastName as Name
						from dbo.Semantic
							join reporting.Global_Resource globalResource on globalResource.ResourceID = Semantic.UpdatedBy
						order by globalResource.FirstName + ' ' + globalResource.LastName";

			using (var connection = ConnectionProvider.Connect(true))
			{
				var results = await connection.QueryAsync(sql);
				return results;
			}
		}

		#region PrivateMethods
		private List<Semantic> findLatestExistingSemantics(List<string> qualifiers, int expectedCount)
		{
			qualifiers = qualifiers.Where(q => !string.IsNullOrEmpty(q)).ToList();
			var query = @" SELECT * FROM Semantic S 
						INNER JOIN (
						SELECT Qualifier, MAX(EffectiveDate) AS EffectiveDate 
						FROM Semantic 
						WHERE Qualifier IN @qualifiers 
						GROUP BY Qualifier
						) M 
						ON M.Qualifier = S.Qualifier AND M.EffectiveDate = S.EffectiveDate";

			using (var connection = ConnectionProvider.Connect(true))
			{
				var existingSemantics = connection.Query<Semantic>(query, new { qualifiers }).ToList();

				if (expectedCount > 0 && existingSemantics.Count != expectedCount)
				{
					var missing = qualifiers.Where(q => !existingSemantics.Any(e => e.Qualifier == q)).ToList();
					throw new GenericException(
						HttpStatusCode.NotFound,
						"Some semantics were not found.",
						$"The following semantics could not be located: {string.Join("; ", missing)}."
						);
				}

				return existingSemantics;
			}

		}

		private string generateTransactionId()
		{
			return "".GenerateNonce(10);
		}

		private void addToChangeLog(string transactionId, string action)
		{
			var query = @"declare @items table (
							Qualifier nvarchar(200),
							Object varchar(50),
							ObjectID bigint,
							ResourceID int,
							[Date] datetime,
							[Action] varchar(15),
							AuditId bigint null,
							[Version] int,
							index ix_OTitems(Qualifier)
						)

						declare @fields table (
							Qualifier nvarchar(200),
							EffectiveDate datetime,
							FieldName varchar(250),
							[Value] nvarchar(max),
							[PreviousValue] nvarchar(max),
							AuditId bigint null
						)

						declare @QualifierAuditID table
						(
							Qualifier nvarchar(200),
							MaxVersion int,
							index ix_OTQualifierAuditID(Qualifier)
							);

						insert into @items
							select	Qualifier,
									'Semantic',
									ID,
									UpdatedBy,
									UpdatedOn,
									case @action
										when 'C' then 'Created'
										when 'U' then 
											case when effectiveDate < UpdatedOn then 'Disabled'
											else'Updated' end											
										else 'Removed'
									end,
									null,
									null
							from	Semantic 
							where	TransactionId = @transactionId

						insert into @fields (Qualifier, EffectiveDate, FieldName, [Value])
							select	S.Qualifier, S.EffectiveDate, F.FieldName, F.FieldValue
							from	Semantic S 
									cross apply (
										select 'Name', cast([Name] as nvarchar(max)) 
										union all select 'Description', cast([Description] as nvarchar(max))
										union all select 'Threshold', cast([Threshold] as nvarchar(max))
										union all select 'Status', cast([Status] as nvarchar(max))
										union all select 'Priority', cast([Priority] as nvarchar(max))
										union all select 'Source', cast([Source] as nvarchar(max))
										union all select 'MatchType', cast(MatchType as nvarchar(max))
										union all select 'BaseType', cast(BaseType as nvarchar(max))
										union all select 'RegularExpression', cast(RegularExpression as nvarchar(max))
										union all select 'ValidValues', cast(ValidValues as nvarchar(max))
										union all select 'InvalidValues', cast(InvalidValues as nvarchar(max))
										union all select 'ValidLocales', cast(ValidLocales as nvarchar(max))
										union all select 'MinimumSamples', cast(MinimumSamples as nvarchar(max))
										union all select 'HeaderFilter', cast(HeaderFilter as nvarchar(max))
										union all select 'HeaderFilterConfidence', cast(HeaderFilterConfidence as nvarchar(max))
										union all select 'Minimum', cast(Minimum as nvarchar(max))
										union all select 'Maximum', cast(Maximum as nvarchar(max))
										union all select 'MinMaxPresent', cast(MinMaxPresent as nvarchar(max))
										union all select 'JsonPayload', cast(JsonPayload as nvarchar(max)) 
										union all select 'Disabled', case when EffectiveDate < UpdatedOn then 'True' else 'False' end
									) F (FieldName, FieldValue)
							where	TransactionId = @transactionId

						if @action = 'U'
						begin
							update	T
							set		T.PreviousValue = FS.FieldValue
							from	@fields T
									cross apply (
												select	S.Qualifier, F.FieldName, F.FieldValue
												from	(
														select	Qualifier,
																max(EffectiveDate) as EffectiveDate
														from	Semantic
														where	Qualifier = T.Qualifier
																and EffectiveDate < T.EffectiveDate
														group by Qualifier
														) MS
														inner join Semantic S on S.Qualifier = MS.Qualifier and S.EffectiveDate = MS.EffectiveDate
														cross apply (
															select 'Name', cast(S.[Name] as nvarchar(max)) 
															union all select 'Description', cast(S.[Description] as nvarchar(max))
															union all select 'Threshold', cast(S.[Threshold] as nvarchar(max))
															union all select 'Status', cast(S.[Status] as nvarchar(max))
															union all select 'Priority', cast(S.[Priority] as nvarchar(max))
															union all select 'Source', cast(S.[Source] as nvarchar(max))
															union all select 'MatchType', cast(S.MatchType as nvarchar(max))
															union all select 'BaseType', cast(S.BaseType as nvarchar(max))
															union all select 'RegularExpression', cast(S.RegularExpression as nvarchar(max))
															union all select 'ValidValues', cast(S.ValidValues as nvarchar(max))
															union all select 'InvalidValues', cast(S.InvalidValues as nvarchar(max))
															union all select 'ValidLocales', cast(S.ValidLocales as nvarchar(max))
															union all select 'MinimumSamples', cast(S.MinimumSamples as nvarchar(max))
															union all select 'HeaderFilter', cast(S.HeaderFilter as nvarchar(max))
															union all select 'HeaderFilterConfidence', cast(S.HeaderFilterConfidence as nvarchar(max))
															union all select 'Minimum', cast(S.Minimum as nvarchar(max))
															union all select 'Maximum', cast(S.Maximum as nvarchar(max))
															union all select 'MinMaxPresent', cast(S.MinMaxPresent as nvarchar(max))
															union all select 'JsonPayload', cast(S.JsonPayload as nvarchar(max)) 
															union all select 'Disabled', case when S.EffectiveDate < S.UpdatedOn then 'True' else 'False' end
														) F (FieldName, FieldValue)
												where	S.Qualifier = T.Qualifier
														and F.FieldName = T.FieldName
												) FS

							delete @fields where [Value] = PreviousValue
						end

						delete @fields where [Value] is null and PreviousValue is null

						declare @ids table (AuditId bigint, Qualifier nvarchar(200))

						;with rs as (select distinct t.Qualifier,S.Id SemanticID
									from @items t
									inner join Semantic S on t.Qualifier = S.Qualifier)
						insert into @QualifierAuditID
						select rs.Qualifier,max(A.[Version]) as [MaxVersion]
						from rs
						inner join [reporting].[Global_Audit] A on A.Object = 'Semantic'  and A.ObjectID = rs.SemanticID
						group by rs.Qualifier;

						update	T
						set		T.Version = S.[MaxVersion] + 1
						from	@items T
								cross apply (
									select	coalesce(A.MaxVersion,0) as [MaxVersion]
									from	@QualifierAuditID A
									where   A.Qualifier = T.Qualifier
								) S


						insert into [reporting].[Global_Audit] (
							Object, ObjectID, ObjectName, 
							ResourceID, Date, Action, 
							ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, 
							ActionDescription,Version)
						output inserted.ID, inserted.ObjectName into @ids
						select	Object, ObjectID, Qualifier, 
								ResourceID, Date, Case Action when 'Disabled' then 'Updated' else Action end,
								Object, ObjectID, 'Semantic', Qualifier,
								'Semantic type ' + case @action
										when 'C' then 'created'
										when 'U' then 
											case when Action = 'Disabled' then 'disabled' 
											else 'updated' end
										else 'removed'
									end + '.', coalesce(Version,1)
						from @items

						update	T
						set		T.AuditId = S.AuditId
						from	@items T
								inner join @ids S on S.Qualifier = T.Qualifier

						update	T
						set		T.AuditId = S.AuditId
						from	@fields T
								inner join @items S on S.Qualifier = T.Qualifier

						insert into [reporting].[Global_FieldAudit] (AuditID, FieldTypeID, FieldName, Value, PreviousValue)
							select AuditID, 0, FieldName, Value, PreviousValue from @fields";

			using (var connection = ConnectionProvider.Connect())
			{
				connection.Open();
				connection.Execute(query, new { transactionId, action });
			}
		}

		private async Task<List<GlobalReportingResource>> GetSemanticCreatorUpdator(string transactionId)
		{
			var qry = $@"
						with rs_data as
						(select distinct * 
						from (
								select CreatedBy ResourceID from Semantic
								where transactionId = @transactionId
								union all
								select UpdatedBy ResourceID from Semantic
								where transactionId = @transactionId
							) a
						)
						select gr.*
						from rs_data s
						inner join [reporting].[Global_Resource] gr on gr.ResourceID = s.ResourceID
			";

			using (var connection = ConnectionProvider.Connect(true))
			{
				var qryresult = await connection.QueryAsync<GlobalReportingResource>(qry, new { transactionId });
				var results = qryresult.ToList();
				return results;
			}

		}

		private string buildStatusOrderSQL()
		{
			StringBuilder statusSQL = new StringBuilder(", CASE");
			foreach (int i in Enum.GetValues(typeof(SemanticStatus)))
			{
				statusSQL.Append($@" WHEN status = {i} then '{Enum.GetName(typeof(SemanticStatus), i)}'");
			}
			statusSQL.Append(" ELSE '' END as statusString");

			return statusSQL.ToString();
		}

		private string insertSemanticQuery()
		{
			return @"
MERGE INTO Semantic AS Target
USING (VALUES (
    @Qualifier, @EffectiveDate, @Name, @Description, @Threshold, @Status, 
    @Priority, @Source, @MatchType, @BaseType, @RegularExpression, 
    @ValidValues, @InvalidValues, @ValidLocales, @MinimumSamples, 
    @HeaderFilter, @HeaderFilterConfidence, @Minimum, @Maximum, 
    @MinMaxPresent, @JsonPayload, @CreatedBy, @CreatedOn, @UpdatedBy, 
    @UpdatedOn, @TransactionId, @Uid
)) AS Source (
    Qualifier, EffectiveDate, Name, Description, Threshold, Status, 
    Priority, Source, MatchType, BaseType, RegularExpression, 
    ValidValues, InvalidValues, ValidLocales, MinimumSamples, 
    HeaderFilter, HeaderFilterConfidence, Minimum, Maximum, 
    MinMaxPresent, JsonPayload, CreatedBy, CreatedOn, UpdatedBy, 
    UpdatedOn, TransactionId, Uid
)
ON Target.Qualifier = Source.Qualifier AND Target.EffectiveDate = Source.EffectiveDate -- Match based on Qualifier
WHEN MATCHED THEN
    UPDATE SET 
        --EffectiveDate = Source.EffectiveDate,
        Name = Source.Name,
        Description = Source.Description,
        Threshold = Source.Threshold,
        Status = Source.Status,
        Priority = Source.Priority,
        Source = Source.Source,
        MatchType = Source.MatchType,
        BaseType = Source.BaseType,
        RegularExpression = Source.RegularExpression,
        ValidValues = Source.ValidValues,
        InvalidValues = Source.InvalidValues,
        ValidLocales = Source.ValidLocales,
        MinimumSamples = Source.MinimumSamples,
        HeaderFilter = Source.HeaderFilter,
        HeaderFilterConfidence = Source.HeaderFilterConfidence,
        Minimum = Source.Minimum,
        Maximum = Source.Maximum,
        MinMaxPresent = Source.MinMaxPresent,
        JsonPayload = Source.JsonPayload,
        UpdatedBy = Source.UpdatedBy,
        UpdatedOn = Source.UpdatedOn,
        TransactionId = Source.TransactionId,
        Uid = Source.Uid
WHEN NOT MATCHED THEN
    INSERT (Qualifier, EffectiveDate, Name, Description, Threshold, Status, 
            Priority, Source, MatchType, BaseType, RegularExpression, 
            ValidValues, InvalidValues, ValidLocales, MinimumSamples, 
            HeaderFilter, HeaderFilterConfidence, Minimum, Maximum, 
            MinMaxPresent, JsonPayload, CreatedBy, CreatedOn, UpdatedBy, 
            UpdatedOn, TransactionId, Uid)
    VALUES (
        Source.Qualifier, Source.EffectiveDate, Source.Name, Source.Description, Source.Threshold, Source.Status, 
        Source.Priority, Source.Source, Source.MatchType, Source.BaseType, Source.RegularExpression, 
        Source.ValidValues, Source.InvalidValues, Source.ValidLocales, Source.MinimumSamples, 
        Source.HeaderFilter, Source.HeaderFilterConfidence, Source.Minimum, Source.Maximum, 
        Source.MinMaxPresent, Source.JsonPayload, Source.CreatedBy, Source.CreatedOn, Source.UpdatedBy, 
        Source.UpdatedOn, Source.TransactionId, Source.Uid
    );";
		}

		private readonly List<string> complexNonSortableFields = new List<string>
		{
			"headerRegExps",
			"invalidList",
			"advanced",
			"regExpReturned",
			"validLocales",
			"validList"
		};

		private readonly Dictionary<string, string> orderFields = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			{ "baseType", "S.BaseType" },
			{ "description", "S.Description" },
			{ "effectiveDate", "S.EffectiveDate" },
			{ "headerRegExpConfidence", "S.HeaderFilterConfidence" },
			{ "matchType", "S.MatchType" },
			{ "maximum", "S.Maximum" },
			{ "minimum", "S.Minimum" },
			{ "minSamples", "S.MinimumSamples" },
			{ "minMaxPresent", "S.MinMaxPresent" },
			{ "name", "S.Name" },
			{ "priority", "S.Priority" },
			{ "qualifier", "S.Qualifier" },
			{ "status", "StatusString" },
			{ "threshold", "S.Threshold" },
			{ "isDisabled", "case when S.EffectiveDate < S.UpdatedOn then 1 else 0 end" }
		};

		#endregion

	}
}
