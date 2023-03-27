using d360.core.enums;
using d360.core.resources;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Newtonsoft.Json;
using Resources;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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

			public bool UseAsSortBy { get; set; }

			public string Sort { get; set; }

		}

		public class CatalogWhere
		{
			public string PropertyName { get; set; }
			public string Query { get; set; }
			public string Where { get; set; }
			public string TokenExpression { get; set; }
			public int PredicateId { get; set; }
		}

		public enum CatalogColumnType
		{
			SystemField, Predicate
		}

		public IEnumerable<int> GetAllIndexes(string source, string matchString)
		{
			matchString = Regex.Escape(matchString);
			foreach (Match match in Regex.Matches(source, matchString))
			{
				yield return match.Index;
			}
		}

		#endregion

		/// <summary>
		/// Retrieves assets across types that are categorized using special relationships.
		/// </summary>
		/// <remarks>
		///Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example: `city eq 'Redmond'`.
		///*  For comparison operators on Text fields you can use eq (equal), ne (not equal), ct (contains), nct (not contains)
		///*  For comparison operators on List fields you can use in (in) and nin (not in)
		///     
		///Example :
		///     
		///- **Text comparing operators ** (displayValue, displayPath)
		///	- Equals operator - {fieldname} eq Data
		///	- Not equals operator - {fieldname} ne Data
		///	- Contains operator - {fieldname} ct Data
		///	- Not Contains operator - {fieldname} nct Data
		///	- Starts with operator - {fieldname} ct Data*
		///	- Ends with operator - {fieldname} ct *Data
		///	
		///- **List Comparison Operators** (Reference List values)
		///	- Equals operator - {fieldname} eq Data
		///	- Not equals operator - {fieldname} neq Data
		///	- Is Populated - {fieldname} eq null
		///	- Is Not Populated - {fieldname} ne null
		///	
		///- ** Logical Operators **
		/// - Logical and - {fieldname} ge 00 and {fieldname} le 99
		/// - Logical or - {fieldname} eq 'Data' or {fieldname} eq 'Data1'
		/// - AssetPath Contains (Match All(and)) - (({AssetPathFieldName} ct 'APValue1') and ({AssetPathFieldName} ct 'APValue2') )
		/// - AssetPath Contains (Match Any(or)) - (({AssetPathFieldName} ct 'APValue1') or ({AssetPathFieldName} ct 'APValue2'))
		///
		///
		///Sorting is done using _order parameter and sort expressions are specified using operator and property name. For example: `desc(displayValue)`.
		///
		///Examples using Ascending Order:
		///- asc(displayPath)
		///- asc(displayValue)
		///
		///Examples using Descending Order:
		///- desc(displayPath)
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
			if (cancellationToken == null)
			{
				cancellationToken = CancellationToken.None;
			}

			var queryParams = Request.GetQueryNameValuePairs();
			string sql = "";
			string advancedFilterString = "";
			string simpleFilterTempTable = "";
			StringBuilder assetsHierarchyTempTable = new StringBuilder();
			int hierarchyMaxDepth = 0;

			List<string> whereStatements = new List<string>();

			// Get relevant predicates.
			sql = "select p.Id, p.Name from [Predicate] p where p.[Type] = 5 and exists (select 1 from IntersectType where PredicateId = p.Id)";
			var predicates = Company.Query<PredValue>(sql).ToList();

			#region Add columns

			var columns = new List<CatalogColumn> {
				new CatalogColumn { ApiName = "uid", Column = "a.Uid", Position = 1 },
				new CatalogColumn { ApiName = "displayValue", Column = "adv.DisplayValue", Position = 2, Sort = "S.ObjectDisplayValue" }
			};

			predicates.ForEach(p =>
			{
				columns.Add(new CatalogColumn
				{
					ApiName = p.Name,
					Column = $@"P{p.Id}.val as [{p.Name}]",
					DataStatement = $"outer apply (select string_agg(SubjectDisplayValue, '; ') from dbo.CatalogBrowseSubject where ObjectAssetID = res.objectassetid and PredicateId = {p.Id})P{p.Id}(val)",
					Position = columns.Max(c => c.Position) + 1,
					JoinStatement = $"left join dbo.CatalogBrowseSubject P{p.Id} on P{p.Id}.ObjectAssetID = S.ObjectAssetID and P{p.Id}.PredicateId = {p.Id}",
					CatalogColumnType = CatalogColumnType.Predicate,
					Sort = $"P{p.Id}.SubjectDisplayValue"
				});
			});

			// Add path as the last column.
			columns.Add(new CatalogColumn { ApiName = "displayPath", Column = "p.DisplayPath", Sort = "p.DisplayPath", JoinStatement = "inner join AssetPath p on p.Id = S.ObjectAssetId", Position = columns.Max(c => c.Position) + 1 });

			#endregion

			#region Filtering logic

			var catalogWheres = new List<CatalogWhere>();
			var dbArgs = new DynamicParameters();
			if (queryParams.Any(q => q.Key == "_filter"))
			{
				advancedFilterString = queryParams.Where(q => q.Key == "_filter").Select(s => s.Value).FirstOrDefault();

				var filters = Regex.Matches(advancedFilterString, @"([\w\s\-\/\:]+)\s(ct|eq|in|nct|neq|nin|ne)\s([\w\s\>\-\/\:\,\*]+)");
				if (filters.Count > 0)
				{
					int parameterIndex = 1;
					foreach (Match filterGrp in filters)
					{
						var filterMatch = Regex.Match(filterGrp.Value, @"^([\w\s\-\/\:]+)\s(ct|eq|in|nct|neq|nin|ne)\s([\w\s\>\-\/\:\,\*]+)$");
						if (filterMatch.Success && filterMatch.Groups.Count == 4)
						{
							var filterProperty = filterMatch.Groups[1].Value;
							var filterOperation = filterMatch.Groups[2].Value;
							var filterValue = filterMatch.Groups[3].Value;
							var column = columns.FirstOrDefault(c => c.ApiName.ToLowerInvariant() == filterProperty.ToLowerInvariant());
							if (column != null)
							{
								// Treat as standard text value.
								var operation = "";
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
									case "ne":
									case "neq":
										operation = "<>";
										dbArgs.Add($"p{parameterIndex}", filterValue);
										break;
								}
								var predicate = predicates.FirstOrDefault(p => p.Name.ToLowerInvariant() == filterProperty.ToLowerInvariant());

								if (predicate != null)
								{
									string valuePart = $"pred.DisplayValue {operation} @p{parameterIndex}";
									string query = $@"select I.ObjectAssetID from #v pred 
											inner join [Intersect] I on I.SubjectAssetID = pred.AssetId
											inner join [IntersectType] IT on IT.ID = I.IntersectTypeID and IT.PredicateID = {predicate.Id}
											where pred.PredicateId = {predicate.Id} and {valuePart}";

									if (filterValue.Trim().ToLowerInvariant() == "null")
									{
										if (operation == "=")
										{
											valuePart = $"pred.DisplayValue is null";
											query = $@"
											    select S.ObjectAssetID from dbo.CatalogBrowseSubject S
												left join dbo.CatalogBrowseSubject SP ON SP.ObjectAssetID = S.objectassetid and SP.PredicateId = {predicate.Id}
												where SP.ObjectAssetID is null";
										}
										else
										{
											valuePart = $"pred.DisplayValue is not null";
										}
									}

									if (filterOperation == "ne")
									{
										query = $@"select I.ObjectAssetID from #v pred 
											inner join [Intersect] I on I.SubjectAssetID = pred.AssetId
											inner join [IntersectType] IT on IT.ID = I.IntersectTypeID and IT.PredicateID = {predicate.Id}
											where pred.PredicateId = {predicate.Id} and {valuePart}
											union
											select S.ObjectAssetID from dbo.CatalogBrowseSubject S
											left join dbo.CatalogBrowseSubject SP ON SP.ObjectAssetID = S.objectassetid and SP.PredicateId = {predicate.Id}
											where SP.ObjectAssetID is null";
									}

									catalogWheres.Add(new CatalogWhere
									{
										TokenExpression = filterGrp.Value,
										PropertyName = $"p{parameterIndex}",
										PredicateId = predicate.Id,
										Where = $"fr.p{parameterIndex} = 1",
										Query = query
									});
								}
								else if (column.ApiName == "displayValue")
								{
									catalogWheres.Add(new CatalogWhere
									{
										TokenExpression = filterGrp.Value,
										PropertyName = $"p{parameterIndex}",
										Where = $"fr.p{parameterIndex} = 1",
										Query = $@"select distinct ObjectAssetId from dbo.CatalogBrowseObject cbo where cbo.ObjectDisplayValue {operation} @p{parameterIndex}"
									});
								}
								else if (column.ApiName.ToLowerInvariant() == "displaypath")
								{
									string query = "";
									if ((filterOperation == "ct" || filterOperation == "nct") && filterValue.Contains("*"))
									{
										//contains and not contains operator needs to use temp table that contains hierarchy of all catalog asset
										//with temp table we can filter out results faster
										if (assetsHierarchyTempTable.Length == 0)
										{
											//find max depth of any asset type that is used CatalogBrowse relationship types
											hierarchyMaxDepth = (await Company.QueryAsync<int>($@"
													select max(Type.Depth) from IntersectTypeDetail ITD
													outer apply (
													select top 1 ap.Segments.value('count(/path/segment)', 'int') - 1 as Depth from Asset a 
													inner join AssetPath AP on AP.ID = a.ID
													where a.AssetTypeID = ITD.ObjectAssetTypeID
													)Type(Depth)
													where ITD.PredicateType = {(int)PredicateType.CatalogBrowse}")).FirstOrDefault();

											if (hierarchyMaxDepth == 0)
											{
												hierarchyMaxDepth = 1;
											}

											List<string> selects = new List<string>();
											List<string> joins = new List<string>();
											for (int i = 1; i <= hierarchyMaxDepth; i++)
											{
												selects.Add($"i{i}.SubjectAssetID as i{i}");
												if (i == 1)
												{
													joins.Add($"left join [Intersect] i{i} on i{i}.ObjectAssetID = r.ObjectAssetID and i{i}.IntersectTypeID in (select ID from #rels)");
												}
												else
												{
													joins.Add($"left join [Intersect] i{i} on i{i}.ObjectAssetID = i{i - 1}.SubjectAssetID and i{i}.IntersectTypeID in (select ID from #rels)");
												}
											}

											//build temporary table that will hold hierarchy asset id's for all catalog assets
											assetsHierarchyTempTable.AppendLine($@"
											drop table if exists #rels
											select itd.ID into #rels 
											from IntersectTypeDetail itd where itd.PredicateType in (3,4);
											create nonclustered index nix_rels_id on #rels (Id);

											drop table if exists #hierarchy
											select distinct r.ObjectAssetID, 
											{string.Join("," + Environment.NewLine, selects)}
											into #hierarchy
											from CatalogBrowseObject r
											{string.Join(Environment.NewLine, joins)}
											option(recompile);");
										}

										//prefilter all asset that match search criteria
										assetsHierarchyTempTable.AppendLine($@"
											drop table if exists #filtered_parents_{parameterIndex}
											select adv.AssetID into #filtered_parents_{parameterIndex}   
											from assettype at
												inner join asset a on a.assettypeid = at.id
												inner join assetdisplayvalue adv on adv.assetid = a.id
											where at.class = {(int)AssetTypeClass.TechnicalAsset} and DisplayValuePrefix like @p{parameterIndex}
											option(recompile);

											CREATE CLUSTERED INDEX ix_tempCIndexAft_{parameterIndex} ON #filtered_parents_{parameterIndex} (AssetId);");


										List<string> hierarchyLevelSearchJoins = new List<string>();
										List<string> hierarchyLevelSearchWheres = new List<string>();
										string whereConnector = " and ";

										hierarchyLevelSearchJoins.Add($"left join #filtered_parents_{parameterIndex} fp_object on fp_object.AssetID = h.ObjectAssetId");
										hierarchyLevelSearchWheres.Add("fp_object.assetid is not null");

										dbArgs.Add($"p{parameterIndex}", $"{filterValue.Replace("*", "%")}");
										for (int i = hierarchyMaxDepth; i > 0; i--)
										{
											hierarchyLevelSearchJoins.Add($"left join #filtered_parents_{parameterIndex} fp{i} on fp{i}.AssetID = h.i{i}");

											if (filterOperation == "ct")
											{
												whereConnector = " or ";
												hierarchyLevelSearchWheres.Add($"fp{i}.assetid is not null");
											}
											else if (filterOperation == "nct")
											{
												whereConnector = " and ";
												hierarchyLevelSearchWheres.Add($"fp{i}.assetid is null");
											}
										}

										//build where query using hierarchy temp table and filteres assets temp table
										query = $@"
											select h.ObjectAssetID from #hierarchy h
											{string.Join(Environment.NewLine, hierarchyLevelSearchJoins)}
											where {string.Join(whereConnector, hierarchyLevelSearchWheres)}";
									}
									else
									{
										string value = filterValue;
										switch (filterOperation)
										{
											case "eq":
											case "ct":
												value = (filterValue + "%").Replace("*", "%").Replace("%%", "%");
												query = $@"select id as ObjectAssetID from assetpath where displaypath like @p{parameterIndex}";
												break;
											case "nct":
												value = (filterValue + "%").Replace("*", "%").Replace("%%", "%");
												query = $@"select id as ObjectAssetID from assetpath where displaypath not like @p{parameterIndex}";
												break;
											case "ne":
												value = filterValue + "%";
												query = $@"select id as ObjectAssetID from assetpath where displaypath not like @p{parameterIndex}";
												break;
										}
										dbArgs.Add($"p{parameterIndex}", value);
									}

									catalogWheres.Add(new CatalogWhere
									{
										TokenExpression = filterGrp.Value,
										PropertyName = $"p{parameterIndex}",
										Where = $"fr.p{parameterIndex} = 1",
										Query = query
									});
								}
							}
						}
						parameterIndex++;
					}
				}
			}

			if (queryParams.Any(q => q.Key == "_simpleFilter"))
			{
				var simpleFilter = queryParams.Where(q => q.Key.ToLowerInvariant() == "_simplefilter").Select(s => s.Value).FirstOrDefault();

				//replace * with contains symbol for sql
				simpleFilter = simpleFilter.Replace("*", "%");

				//add % to make sure it is using at least startsWith
				simpleFilter += "%";

				//if last step did cause duplicate %, remove one
				simpleFilter = simpleFilter.Replace("%%", "%");

				dbArgs.Add("simpleFilter", simpleFilter);
				simpleFilterTempTable = $@"
					drop table if exists #simpleFiltersTempTable

					select ObjectAssetID
					into #simpleFiltersTempTable
					from dbo.CatalogBrowseSubject where SubjectDisplayValue like @simpleFilter

					insert into #simpleFiltersTempTable
					select ObjectAssetID from dbo.CatalogBrowseObject where ObjectDisplayValue like @simpleFilter

					insert into #simpleFiltersTempTable 
					select ObjectAssetID from dbo.CatalogBrowseObject
					inner join AssetPath ap on ap.ID = ObjectAssetID
					where ap.DisplayPath like @simpleFilter

					create nonclustered index idx on #simpleFiltersTempTable (ObjectAssetID)";
			}

			#endregion

			#region Sort logic

			var parsedSorts = new Dictionary<string, bool>();
			if (queryParams.Any(q => q.Key == "_order"))
			{
				var rawSorts = queryParams.Where(q => q.Key == "_order").Select(s => s.Value).ToList();
				rawSorts.ForEach(rawSort =>
				{
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
				var column = columns.Where(x => !string.IsNullOrEmpty(x.Sort)).FirstOrDefault(c => c.ApiName.ToLowerInvariant() == key.ToLowerInvariant());
				if (column != null)
				{
					sorts.Add($"{column.Sort} {sortDirection}");
					column.UseAsSortBy = true;
				}
				else
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format(AssetsApiMessages.InvalidSortDataCatalog, key, string.Join(", ", columns.Where(x => !string.IsNullOrEmpty(x.Sort)).Select(x => x.ApiName)))));
				}
			}

			if (sorts.Count == 0)
			{
				sorts.Add($"S.ObjectDisplayValue asc");
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
			string definitionSql = "";
			if (includeDefinition)
			{
				definitionSql = "select PredicateId as Id, DisplayValue as Name from #v;";
			}

			bool hasFilters = catalogWheres.Count > 0;

			string filtersTempTable = "";
			if (hasFilters)
			{
				StringBuilder sb = new StringBuilder();
				List<string> tempCols = new List<string>
				{
					"AssetId bigint"
				};
				catalogWheres.ForEach(w =>
				{
					tempCols.Add($"{w.PropertyName} bit");
				});
				sb.AppendLine($@"
					drop table if exists #filteredResults
					create table #filteredResults ({string.Join(",", tempCols)});");

				foreach (var filter in catalogWheres)
				{
					sb.AppendLine($@"
						MERGE #filteredResults AS Target
						USING (
						{filter.Query}
						) AS Source
						ON Source.ObjectAssetID = Target.AssetId
						WHEN MATCHED THEN UPDATE SET
							Target.{filter.PropertyName} = 1
						WHEN NOT MATCHED BY Target THEN
							INSERT (AssetId,{filter.PropertyName}) 
							VALUES (Source.ObjectAssetID, 1)
						option(recompile);");
				}

				filtersTempTable = sb.ToString();
			}

			if (catalogWheres.Count > 0)
			{
				//replaced original query parameter value which contains all brackets and and/or operators with parsed where values
				//order by TokenExpression to avoid wrong replacement in similiar expressions i.w. field ct test and field ct testing
				foreach (var cwhere in catalogWheres.OrderByDescending(x => x.TokenExpression.Length))
				{
					advancedFilterString = advancedFilterString.Replace(cwhere.TokenExpression, cwhere.Where);
				}

				whereStatements.Add(advancedFilterString);
			}

			string permissionTempTableSql = "";

			if (!Company.CurrentResourceIsAdmin)
			{
				permissionTempTableSql = $@"
					drop table if exists #NoReadAssets;
					create table #NoReadAssets(
						AssetId int,
						AssetTypeID bigint,
						PermissionsBitMask int
					)

					create index cix_permissionAssetId on #NoReadAssets(Assetid);


					declare @assetTypeIds table (id int, PermissionsBitMask int)
					insert into @assetTypeIds
					select distinct ObjectAssetTypeID, 0 from [Predicate] P 
					inner join  [IntersectType] IT on IT.PredicateID = P.ID
					where P.Type = {((int)PredicateType.CatalogBrowse)}

					declare @typeid int;
					set @typeid = (select top 1 id from @assetTypeIds)
					while @typeid is not null
					begin
						insert into #NoReadAssets
						select AssetID,AssetTypeID,PermissionsBitMask from dbo.UserAssetPermissions(@userId,@typeid) where ((PermissionsBitMask & {((int)Permission.ReadAsset)})) = 0; 

						delete top (1) from @assetTypeIds
						set @typeid = (select top 1 id from @assetTypeIds)
					end


					insert into @assetTypeIds (id, PermissionsBitMask)
					select distinct AssetTypeID, PermissionsBitMask from #NoReadAssets where AssetId = 0

					insert into #NoReadAssets
					select a.ID, ati.id, ati.PermissionsBitMask from @assetTypeIds ati
					inner join asset a on a.AssetTypeID = ati.id";

				dbArgs.Add("userId", Company.CurrentResourceID);
				whereStatements.Add("not exists (select AssetID from #NoReadAssets where AssetID = S.ObjectAssetID)");
			}


			string whereStatement = whereStatements.Count > 0 ? " where " + string.Join(" and ", whereStatements) : "";

			string baseSQL = $@"
				select {{0}}
				from
				dbo.CatalogBrowseObject S
				{(hasFilters ? "inner join #filteredResults fr on fr.AssetId = S.ObjectAssetID" : "")}
				{(!string.IsNullOrEmpty(simpleFilterTempTable) ? "inner join #simpleFiltersTempTable sftt on sftt.ObjectAssetID = s.ObjectAssetID" : "")}
				{string.Join(Environment.NewLine, columns.Where(x => !string.IsNullOrEmpty(x.JoinStatement) && x.UseAsSortBy == true).Select(x => x.JoinStatement).Distinct())}
				{whereStatement}";


			string countSql = $@"
				;with cte as (
				{string.Format(baseSQL, "count(1) as cnt")}
				group by S.ObjectAssetID
				)
				select COUNT(1) from cte
				option(recompile);";


			if (!hasFilters)
			{
				string where = "";
				if (!Company.CurrentResourceIsAdmin)
				{
					where = " where not exists (select AssetID from #NoReadAssets where AssetID = S.ObjectAssetID)";
				}

				countSql = $@"select COUNT(distinct S.ObjectAssetID) from dbo.CatalogBrowseSubject S {where} option(recompile)";

				if (!string.IsNullOrEmpty(simpleFilterTempTable))
				{
					countSql = $@"
						select COUNT(distinct S.ObjectAssetID) 
						from dbo.CatalogBrowseSubject S
						inner join #simpleFiltersTempTable sftt on sftt.ObjectAssetID = S.ObjectAssetID
						{where}
						option(recompile)";
				}
			}


			string offsetGroupBy = "group by S.ObjectAssetId";

			if (columns.Any(x => x.UseAsSortBy))
			{
				offsetGroupBy += ", " + string.Join(", ", columns.Where(x => x.UseAsSortBy).Select(x => x.Sort));
			}

			string resultsSql = $@"
				declare @results table (objectassetid int);

				insert into @results
				{string.Format(baseSQL, "MAX(S.ObjectAssetId) AS ObjectAssetId")}
				{offsetGroupBy}
				order by {string.Join(", ", sorts)}
				{offset}";

			string finalSql = $@"
				drop table if exists #v 
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

				{assetsHierarchyTempTable}

				{permissionTempTableSql}

				{simpleFilterTempTable}	

				{definitionSql}

				{filtersTempTable}

				{countSql}

				{resultsSql}

				select {string.Join("," + Environment.NewLine, columns.Select(x => x.Column))}
				from @results res
				inner join AssetDisplayValue adv on adv.AssetID = res.objectassetid
				inner join asset a on a.ID = res.objectassetid
				inner join AssetPath p on p.Id = A.Id
				{string.Join(Environment.NewLine, columns.Where(x => x.CatalogColumnType == CatalogColumnType.Predicate).Select(x => x.DataStatement).Distinct())}
				option(recompile)
				";

			SqlMapper.GridReader results = await Company.Database.Connection.QueryMultipleAsync(
					  new CommandDefinition(finalSql,
					  cancellationToken: cancellationToken,
					  parameters: dbArgs,
					  commandTimeout: ApiTimeout
					));

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