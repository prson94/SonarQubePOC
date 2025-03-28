using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.core.search;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using repositories;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.V2
{
	/// <summary>
	/// This service houses all endpoints handling search in Govern.
	/// </summary>
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/search"),
		Authorize,
		StringEnum,
		ApiExplorerSettings(IgnoreApi = false)
	]
	public class SearchController : BaseV2ApiController
	{
		private readonly IAssetRepository AssetRepository;
		private readonly IQueueSource Queue;
		private readonly ISemanticsRepository SemanticsRepository;
		private readonly TelemetryClient Telemetry;

		//Icons set based on main Nav item for category
		private readonly Dictionary<string, string> siteNavMap;

		ISearch Search;

		public SearchController(ICoreComponentSet set, ISearch search, IAssetRepository assetRepository, ISemanticsRepository semanticsRepository, IQueueSource queue) : base(set)
		{
			AssetRepository = assetRepository;
			Queue = queue;
			SemanticsRepository = semanticsRepository;
			Telemetry = new TelemetryClient();

			Search = search;

			siteNavMap = new Dictionary<string, string> {
				{ Label.AssetTypeClass_Business, "#Business" },
				{ Label.AssetTypeClass_Technical, "#Technical" },
				{ Label.AssetTypeClass_Model, "#Models" },
				{ Label.AssetTypeClass_Reference, "#Reference" },
				{ Label.AssetTypeClass_Rule, "#Data Quality" },
				{ Label.AssetTypeClass_Policy, "#Policy" }//,
				//{ Label.AssetTypeClass_SemanticType, "#SemanticTypes" }
			};
		}

		void loadSearchResultUris(List<SearchResult> results)
		{
			results.ForEach(r =>
			{
				if (string.IsNullOrEmpty(r.Icon))
				{
					r.Icon = "fa-book";
				}

				if (r.Class != AssetTypeClass.Reference)
				{
					r.Url = $"/asset/{r.Uid}";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/asset/{r.Uid}";
				}
				else
				{
					r.Url = $"/reference/{r.AssetTypeUid}/items";
					r.AbsoluteUrl = $"https://{SecurityContext.CompanyPrefix}.data3sixty.com/reference/{r.AssetTypeUid}/items";
				}
			});
		}

		/// <summary>
		/// Simple phrase search. Returns up to 200 results
		/// </summary>
		/// <param name="phrase">Search term</param>
		/// <returns></returns>
		[
			HttpGet,
			Route(""),
			SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "A list of matching search items.", typeof(IQueryable<IndexResult>)),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE),
		]
		public async Task<IHttpActionResult> GetSearchResults(string phrase)
		{
			if (!string.IsNullOrEmpty(phrase))
			{
				//var queryParams = Request.GetQueryNameValuePairs();
				//int pageNumber = queryParams.CheckForPageNumber();
				//int pageSize = queryParams.CheckForPageSize();
				//int offset = (pageNumber - 1) * pageSize;
				//int take = pageSize;
				var response = await Search.ReadResultsAsync(phrase, false, true, false, false, null, null, 0, 200);
				loadSearchResultUris(response.Data.Results);

				return Ok(response.Data);				
			}

			return Ok();
		}

		/// <summary>
		/// Global Search
		/// </summary>
		/// <remarks>
		/// Perform a search for any assets matching the search term.
		/// 
		/// If the Aggregations parameter is specified, the search will also return aggregate results as is shown in the Filters tab. The parameter is a comma separated list. Currently only the value "category" is supported.
		/// 
		/// AggregationFilters are filters applied to values returned from the Aggregation query. Filters are supported for "Category" and "AssetType"
		/// 
		/// FieldFilters are filters like the advanced filter bar. Fields supported are
		/// *   Name
		/// *   Description
		/// *   Path
		/// *   Tags
		/// </remarks>
		/// <param name="queryRequest">Search Query Request</param>
		/// <returns></returns>
		[
			HttpPost,
			Route("results"),
			SwaggerConsumes("application/json"),
			SwaggerProduces("application/json", "application/octet-stream"),
			SwaggerResponse(HttpStatusCode.OK, "Search results matching the query.", typeof(IndexResults)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your search request is invalid"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public async Task<IHttpActionResult> ReadResultsAsync(QueryRequest queryRequest)
		{
			var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
			
			string isValid = await ValidateQueryRequest(queryRequest);

			if (!string.IsNullOrEmpty(isValid))
			{
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, isValid);
			}

			HttpResponseMessage response;

			List<AssetTypeClass> classes = null;
			List<Guid> types = null;

			// Parse Aggregate Filters by looking at raw incoming text.
			if (queryRequest.AggregationFilters.Count > 0)
			{
				var rawClasses = queryRequest.AggregationFilters.Where(f => f.Class.HasValue);
				if (rawClasses.Count() > 0)
				{
					var classList = AssetTypeClass.BusinessAsset.GetAsList();
					foreach (var rawClass in rawClasses)
					{
						var assetTypeClass = classList.FirstOrDefault(cl => cl.ID == rawClass.Class);
						if (assetTypeClass != null)
						{
							if (classes == null) classes = new List<AssetTypeClass>();
							classes.Add(assetTypeClass.ID);
						}
					}
				}

				types = queryRequest.AggregationFilters
					.Where(f => f.Uid.HasValue && f.Uid != Guid.Empty)
					.Select(f => f.Uid.Value)
					.ToList();
			}

			var dbResponse = await Search.ReadResultsAsync(
				queryRequest.Term, 
				true, true, true, 
				queryRequest.IncludeAggregations, 
				classes, 
				types, 
				queryRequest.From, queryRequest.Size
				);
			
			loadSearchResultUris(dbResponse.Data.Results);

			if (isStreamResponse)
			{
				SLDocument document = SearchResultsAsExcel(dbResponse.Data.Results);
				// Select the first worksheet as the active one.
				var firstSheet = document.GetWorksheetNames()[0];
				document.SelectWorksheet(firstSheet);

				var stream = new MemoryStream();
				document.SaveAs(stream);

				response = createFileResponseMessage(HttpStatusCode.OK, "SearchResults.xlsx", stream.ToArray());
			}
			else
			{
				response = Request.CreateResponse(HttpStatusCode.OK, dbResponse.Data);
			}

			return ResponseMessage(response);
		}

		/// <summary>
		/// Typeahead search suggestions.
		/// </summary>
		/// <remarks>
		/// The typeahead search is indented to provide suggestions based on a partial search term.
		/// 
		/// This search looks for partial matches in Name and Tags.
		/// 
		/// If the partial search term appears to be a UID, the Asset UID and Tag UIDs are searched instead.
		/// </remarks>
		/// <param name="query">Query string</param>
		/// <param name="categories">Comma separated list of Categories to limit search to</param>
		/// <param name="num">Max number of results. Defaults to 7</param>
		/// <returns></returns>
		[
			HttpGet,
			Route("typeahead"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Search result suggestions based on query.", typeof(IList<TypeaheadResult>)),
			SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your search request is invalid"),
			SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public async Task<IHttpActionResult> GetTypeaheads(string query, string categories = null, int? num = null)
		{
			if (!string.IsNullOrWhiteSpace(categories))
			{
				var categoryList = categories.Split(',')
					.Select(c => c.Trim())
					.Where(o => Enum.TryParse<AssetTypeClass>(o, out _))
					.Select(o => (AssetTypeClass)Enum.Parse(typeof(AssetTypeClass), o));

				var invalidCategories = categoryList.Except(await GetVisibleCategories());

				if (invalidCategories.Any())
				{
					return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, string.Format(Error.CategoryNotAvailable, string.Join(", ", invalidCategories)));
				}
			}

			var typeaheadQuery = $"\"{query}*\"";

			var response = await Search.ReadResultsAsync(typeaheadQuery, false, true, false, false, null, null, 0, num ?? 7);
			loadSearchResultUris(response.Data.Results);
			return Ok(response.Data.Results);
		}

		/// <summary>
		/// List categories available for filtering in search
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("categories"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Categories available for filtering", typeof(List<string>)),
			ApiExplorerSettings(IgnoreApi = false)
		]
		public async Task<IHttpActionResult> GetCategories()
		{
			var visibleCategories = await GetVisibleCategories();
			return Ok(visibleCategories.Select(o => o.GetName()));
		}

		/// <summary>
		/// List Categories and Asset Types that can be indexed
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("indexableTypes"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of indexable Categories and Asset Types", typeof(List<IndexableType>)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true),
			RequireAdminPermissions
		]
		public async Task<IHttpActionResult> GetIndexableTypes()
		{
			List<IndexableType> types = (await Company.QueryAsync<IndexableType>(
				@"SELECT at.Name, at.Class, at.Uid as AssetTypeUid, P.[Path] as AssetTypePath
				FROM [dbo].[AssetType] at
				cross apply dbo.GetAssetTypeTextPathById(AT.ID, ' > ') P 
				WHERE EXISTS (SELECT 1 FROM [dbo].[Asset] a WHERE a.AssetTypeId = at.ID)")).ToList();
			
			List<IndexableType> classes = assetTypeClasses
				.Where(c => types.Any(at => at.Class == (int)c))
				.Select(c => new IndexableType { 
					Name = c.ToString(), 
					Class = (int)c, 
					AssetTypeUid = Guid.Empty, 
					ClassName = c.ToString() 
				}).ToList();

			//if (await GetFeatureFlagValue(FlagList.SEMANTIC_TYPES_API))
			//{
			//	classes.Add(new IndexableType { Name = AssetTypeClass.SemanticType.ToString(), Class = (int)AssetTypeClass.SemanticType, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.SemanticType.ToString() });
			//}

			//Overload "Predicate" class as a representation for synonyms
			//classes.Add(new IndexableType { Name = "Synonym", Class = (int)AssetTypeClass.Predicate, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.Predicate.ToString() });

			classes.AddRange(types);

			//Reclassify Reference/ReferenceItemType
			classes.Where((c) => c.Class == 9).ToList().ForEach((c) => c.Class = 14);

			return Ok(classes);
		}

		#region Enrich elastic results with DB data

		private static readonly List<AssetTypeClass> assetTypeClasses = new List<AssetTypeClass> {
			AssetTypeClass.BusinessAsset,
			AssetTypeClass.TechnicalAsset,
			AssetTypeClass.Diagram,
			AssetTypeClass.Model,
			AssetTypeClass.Policy,
			AssetTypeClass.Rule,
			AssetTypeClass.Group,
			AssetTypeClass.User,
			AssetTypeClass.Reference
		};

		private static readonly List<string> supportedAggregations = new List<string>
		{
			"category"
		};

		private static readonly List<string> supportedAggregationFilters = new List<string>
		{
			"AssetType",
			"Category"
		};

		private static readonly List<string> supportedFields = new List<string>
		{
			"Name",
			"Description",
			"Path",
			"Tags",
			"Semantictype"
		};

		//Icons set based on Category/Class directly
		private static readonly Dictionary<string, string> categoryMap = new Dictionary<string, string> {
			{ Label.AssetTypeClass_User, "fa-user" },
			{ Label.AssetTypeClass_Group, "fa-users" },
			{ Label.AssetTypeClass_GramaticType, "fa-comments" },
			{ Label.AssetTypeClass_DiagramAsset, "fa-share-alt" }
		};

		private async Task<List<AssetTypeClass>> GetVisibleCategories()
		{
			var exclude = new List<AssetTypeClass> { AssetTypeClass.Group, AssetTypeClass.User, AssetTypeClass.Diagram };

			List<AssetTypeClass> visibleCategories = assetTypeClasses
				.Where(c => Company.AssetTypes.Any(at => at.Class == c && !exclude.Contains(at.Class)))
				.Select(c => c).ToList();

			//if (Company.Semantics.Any() && FeatureFlags.IsThisTrue(FlagList.PERM_SEMANTIC_TYPES_API, await GetFeatureFlagUser()))
			//{
			//	visibleCategories.Add(AssetTypeClass.SemanticType.ToString());
			//}

			////We have Grammatic Types if we have Nyms or any intersects with predicate type 6
			//if (Company.Nyms.Any())
			//{
			//	visibleCategories.Add("Synonym");
			//}
			//else if (Company.Query<int>(@"select case when exists(select *
			//		from [intersect] I
			//		inner join IntersectType T on T.ID = I.IntersectTypeID
			//		inner join Predicate P on P.ID = T.PredicateID and P.Type = 6) then 1
			//		else 0 end").FirstOrDefault() == 1)
			//{
			//	visibleCategories.Add("Synonym");
			//}

			return visibleCategories;
		}

		private async Task<List<IndexableCount>> GetDatabaseCounts()
		{
			var semanticTypesEnabled = await GetFeatureFlagValue(FlagList.SEMANTIC_TYPES_API);

			var sql = $@"WITH AssetTypesCTE (Class, AssetTypeUid, CurrentCount)
				as
				(
					select at.Class, at.Uid, count(*)
					from assettype at
					inner join asset a on a.AssetTypeID = at.id
					where at.class in (
						{(int)AssetTypeClass.BusinessAsset}, {(int)AssetTypeClass.Model}, {(int)AssetTypeClass.Policy},
						{(int)AssetTypeClass.Rule}, {(int)AssetTypeClass.TechnicalAsset}, {(int)AssetTypeClass.Group},
						{(int)AssetTypeClass.Diagram}
					)
					group by at.Class, at.Uid
					union all
					select at.Class, at.Uid, count(*)
					from assettype at
					inner join asset a on a.AssetTypeID = at.id
					inner join reporting.Global_Resource u on a.ObjectID = u.ResourceID
					where at.class = {(int)AssetTypeClass.User} and a.State = {(int)State.Active} and u.State = {(int)CompanyResourceState.Active}
					group by at.Class, at.Uid
					union all
					select Class, Uid, 1
					from assettype
					where Class = {(int)AssetTypeClass.Reference}
				)
				select Class, AssetTypeUid, CurrentCount
				from AssetTypesCTE
				union all
				select Class, '00000000-0000-0000-0000-000000000000', sum(CurrentCount)
				from AssetTypesCTE
				group by Class
				union all
				SELECT 17, '00000000-0000-0000-0000-000000000000', sum(cnt) from (
					select count(*) * 2 as cnt
					from [dbo].[intersect] I
					inner join IntersectType T on T.ID = I.IntersectTypeID
					inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
					union all
					select count(*) as cnt
					from [dbo].[nym]
				) Syns";

			if(semanticTypesEnabled)
			{
				sql += @" union all
				SELECT 18, '00000000-0000-0000-0000-000000000000', sum(cnt) from(
					select count(distinct Qualifier) as cnt

					from[dbo].[semantic]
				) Sems";
			}

			List<IndexableCount> dbCounts = Company.Query<IndexableCount>(sql).ToList();

			//Reclassify Reference/ReferenceItemType
			dbCounts.Where((c) => c.Class == 9).ToList().ForEach((c) => c.Class = 14);

			return dbCounts;
		}

		private async Task<string> ValidateQueryRequest(QueryRequest queryRequest)
		{
			if (queryRequest.Size > 5000)
			{
				return string.Format(Error.SizeTooBig, 5000);
			}

			if (queryRequest.AggregationFilters.Any())
			{
				if (queryRequest.AggregationFilters.Exists(f => f.Class.HasValue))
				{
					IEnumerable<AssetTypeClass> categoryList = queryRequest.AggregationFilters.Where(f => f.Class.HasValue).Select(c => c.Class.Value);
					IEnumerable<AssetTypeClass> invalidCategories = categoryList.Except(await GetVisibleCategories());
					if (invalidCategories.Any())
					{
						return string.Format(Error.CategoryNotAvailable, string.Join(", ", invalidCategories));
					}
				}
			}

			return "";
		}

		private async Task AppendIcons(IEnumerable<TypeaheadResult> results)
		{
			//Assign icons by Asset Type style
			if (results.Where(r => r.MissingIcon() && r.AssetTypeUid != null).Any())
			{
				var sql = @"select AT.Uid, S.Icon
				from assettypestyle S
				inner join assettype AT on s.id = at.id
				inner join @uids U on U.Uid = AT.Uid
				where S.icon is not null";
				var styles = await Company.QueryAsync<(Guid uid, string icon)>(sql, new
				{
					uids = results.Where(r => r.MissingIcon() && r.AssetTypeUid != null).Select(r => r.AssetTypeUid.ToString()).Distinct().AsTableValuedParameter(
							"dbo.UidTable",
							new List<string> { "Uid" })
				});

				foreach (var s in styles)
				{
					foreach (var r in results.Where(r => r.AssetTypeUid == s.uid))
					{
						r.Icon = s.icon;
					}
				}
			}

			//Assign icons by lower level navmenu
			if (results.Where(r => r.AssetTypeUid != null && r.MissingIcon()).Any())
			{
				var sql = $@"WITH cteParents(AssetTypeUid, ObjectAssetTypeID, SubjectAssetTypeID, Level) as (
					select	at.Uid as AssetTypeUid, it.ObjectAssetTypeID, it.SubjectAssetTypeID, 1
					from	IntersectType it 
							inner join [Predicate] p ON p.ID = it.PredicateID AND p.Type = 3
							inner join AssetType at ON at.ID = it.ObjectAssetTypeID
							inner join @uids U on U.Uid = AT.Uid
					UNION ALL
					select	cteParents.AssetTypeUid, cteParents.ObjectAssetTypeID, it.SubjectAssetTypeID, cteParents.Level+1
					from	cteParents
							inner join IntersectType it ON it.ObjectAssetTypeID = cteParents.SubjectAssetTypeID 
							inner join [Predicate] p ON P.ID = it.PredicateID and p.Type = 3
					)

					select	cteParents.AssetTypeUid, nav.Icon, nav.ImageIconUrl
					from	cteParents
							inner join AssetType sat on sat.ID = cteParents.SubjectAssetTypeID
							inner join SiteNav nav1 on sat.[Object] = nav1.Object and sat.ObjectID = nav1.ObjectID
							inner join SiteNav nav on nav1.ParentID = nav.ID
					UNION ALL
					select	at.Uid as AssetTypeUid, nav.Icon, nav.ImageIconUrl
					from	AssetType at
							inner join SiteNav nav1 on at.Object = nav1.Object and at.ObjectID = nav1.ObjectID
							inner join SiteNav nav on nav1.ParentID = nav.ID
							inner join @uids U on U.Uid = AT.Uid;";

				var menuItems = Company.Query<dynamic>(sql, new
				{
					uids = results.Where(r => r.AssetTypeUid != null && r.MissingIcon()).Select(r => r.AssetTypeUid.ToString()).Distinct().AsTableValuedParameter(
							"dbo.UidTable",
							new List<string> { "Uid" })
				});

				var baseUri = Config.GetStorageUrl(constants.Storage.Resources);
				foreach (var m in menuItems)
				{
					foreach (var r in results.Where(r => r.AssetTypeUid == m.AssetTypeUid))
					{
						if (!string.IsNullOrEmpty(m.ImageIconUrl))
						{
							r.ImageUrl = $"{baseUri}/{ m.ImageIconUrl}";
						}
						else if (!string.IsNullOrEmpty(m.Icon))
						{
							if (((string)m.Icon).StartsWith("/"))
							{
								r.ImageUrl = m.Icon;
							}
							else
							{
								r.Icon = m.Icon;
							}
						}
					}
				}
			}

			//Assign icons based on category
			foreach (var r in results.Where(res => res.MissingIcon() && categoryMap.Keys.Contains(res.Group)))
			{
				r.Icon = categoryMap[r.Group];
			}

			//Assign icons from sitenav based on category
			if (results.Any(r => r.MissingIcon() && siteNavMap.Keys.Contains(r.Group)))
			{
				var sql = "select Name, Icon, ImageIconUrl FROM [dbo].[SiteNav] WHERE Name in @names";
				var names = siteNavMap.Values.ToList();
				Dictionary<string, (string, string)> iconMap = Company.Query<(string Name, string Icon, string ImageIconUrl)>(sql, new { names }).ToDictionary(t => t.Name, t => (t.Icon, t.ImageIconUrl));

				var baseUri = Config.GetStorageUrl(constants.Storage.Resources);
				foreach (var r in results.Where(res => res.MissingIcon() && siteNavMap.ContainsKey(res.Group) && iconMap.ContainsKey(siteNavMap[res.Group])))
				{
					if (!string.IsNullOrEmpty(iconMap[siteNavMap[r.Group]].Item2))
					{
						r.ImageUrl = $"{baseUri}/{iconMap[siteNavMap[r.Group]].Item2}";
					}
					else if (!string.IsNullOrEmpty(iconMap[siteNavMap[r.Group]].Item1))
					{
						r.Icon = iconMap[siteNavMap[r.Group]].Item1;
					}
				}
			}

			//Assign default icon to any result missing at this point
			foreach (var r in results.Where(res => res.MissingIcon()))
			{
				r.Icon = "fa-circle-o";
			}
		}

		private async Task AppendPaths(IEnumerable<TypeaheadResult> results)
		{
			Dictionary<Guid, List<PathComponent>> paths = await AssetRepository.GetAssetPathComponents(results.Where(r => r.Uid.HasValue).Select(r => r.Uid ?? Guid.Empty).ToList());
			foreach (var r in results.Where(r => r.Uid.HasValue))
			{
				Guid uid = r.Uid ?? Guid.Empty;
				
				if (paths.ContainsKey(uid))
				{
					r.AssetPath = paths[uid];
				}
			}
		}

		private async Task AugmentResults(IEnumerable<TypeaheadResult> results)
		{
			await AppendIcons(results).ConfigureAwait(false);
			await AppendPaths(results).ConfigureAwait(false);
		}

		private List<Guid> GetAssetTypeUidWithField(IEnumerable<IndexResult> results)
		{
			return Company.Query<Guid>(
				@"SELECT at.uid
				FROM assettype at
				INNER JOIN @uids U on U.Uid = AT.Uid
				WHERE exists (select 1 from fieldtype ft where ft.AssetTypeID = at.id and ft.SearchAddToResult = 1)", new
				{
					uids = results.Where(r => r.AssetTypeUid != null).Select(r => r.AssetTypeUid.ToString()).Distinct().AsTableValuedParameter(
						"dbo.UidTable",
						new List<string> { "Uid" })
				})
				.ToList();
		}

		private async Task AugmentResults(IEnumerable<IndexResult> results)
		{
			await AugmentResults(results as IEnumerable<TypeaheadResult>).ConfigureAwait(true);

			if (!results.Any())
			{
				return;
			}

			//Determine which results have asset tyoes with search fields defined
			List<Guid> assetTypeUidWithFields = GetAssetTypeUidWithField(results);

			//Get enrichment values and scores
			Dapper.SqlMapper.GridReader augmentReader = await Company.QueryMultipleAsync(
				@"select
					A.[UID] as [AssetUid],
					COALESCE(StatusColor.FormattedValue, f.FormattedValue, ft.DefaultFormattedValue) as Status,
					A.Object,
					A.ObjectId,
					Profiling.HasProfiling as HasProfiling
				from Asset A
				inner join @uids u on a.uid = u.uid
				inner join AssetType AT on AT.ID = A.AssetTypeID
				left join FieldType ft on ft.AssetTypeID = AT.id and ft.FriendlyName like 'status'
				left Join Field f on f.FieldTypeID = ft.ID and f.AssetID = A.ID
				outer apply (select case when exists (select 1 from AssetDataProfile where AssetID = A.ID) then cast(1 as bit) else cast(0 as bit) end as HasProfiling) Profiling
				outer apply(
					select FormattedValue = 
					(SELECT F.FormattedValue as name,
					COALESCE(JSON_VALUE(ACJF.ColorJSON,'$.Value'), 'transparent') as color FOR JSON PATH) 
					FROM Asset ACF    
					cross apply dbo.GetAssetColorJsonByColor(ACF.Color) ACJF
					WHERE ACF.Object = ft.LookupObjectType and ACF.ObjectID = TRY_PARSE(F.Value as int)
				) StatusColor (FormattedValue);

				select
					O.AssetUid,
					O.EffectiveDate,
					O.EndDate,
					O.Rundate,
					O.ScoreType,
					O.Value,
					O.LowerThreshold,
					O.UpperThreshold
				from (
						select  S.AssetUid,
								S.EffectiveDate,
								S.EndDate,
								S.RunDate,
								case 
									when AL.ScoreType = 1 then 'Governance'
									when AL.ScoreType = 2 then 'DataQuality'
								end as ScoreType,
								ROW_NUMBER() OVER(PARTITION BY S.AssetUid, AL.ScoreType ORDER BY S.EffectiveDate DESC) as RowNum,
								S.Value, 
								AL.LowerThreshold, 
								AL.UpperThreshold 
						from    metrics.Score S
								inner join @uids U on U.Uid = S.AssetUid  and S.EffectiveDate <= GETUTCDATE() 
								inner join metrics.Allocation AL on AL.Uid = S.AllocationUid
						) O
				where	O.RowNum = 1;",
				new
				{
					uids = results
							.Where(r => r.Uid != null)
							.Select(r => r.Uid.ToString())
							.Distinct()
							.AsTableValuedParameter("dbo.UidTable", new List<string> { "Uid" })
				}
			);

			List<SearchAugment> searchAugments = augmentReader.Read<SearchAugment>().ToList();
			List<IndexAssetScore> searchScores = augmentReader.Read<IndexAssetScore>().ToList();

			foreach (var result in results.Where(r => r.Uid.HasValue && r.Uid.Value != Guid.Empty))
			{
				if (assetTypeUidWithFields.Contains(result.AssetTypeUid ?? Guid.Empty))
				{
					result.Fields = await AssetRepository.GetAssetSearchFields(result.Uid ?? Guid.Empty);
				}

				SearchAugment augment = searchAugments.Find(a => a.AssetUid == result.Uid);

				if (augment.AssetUid != Guid.Empty)
				{
					result.Status = augment.Status;
					result.Object = augment.Object;
					result.ObjectId = augment.ObjectId;
					result.HasProfiling = augment.HasProfiling;
				}

				result.Scores = searchScores.Where(s => s.AssetUid == result.Uid).ToList();				
			}
			
			List<string> qualifiers = results.Where(r => r.Group == "Semantic Type").Select(r => r.ID.Substring(r.ID.IndexOf("|") + 1)).ToList();
			List<Semantic> semantics = SemanticsRepository.GetSemanticsByQualifiers(qualifiers);

			if (semantics.Any())
			{
				foreach (var result in results.Where(r => r.Group == "Semantic Type"))
				{
					result.Object = "Semantic Type";

					var semantic = semantics.Find(s => s.Uid == result.Uid);

					if (semantic != null)
					{
						result.AssetPath = new List<PathComponent> { new PathComponent {
						AssetType = null,
						Key = new string[] { semantic.Name }
					}};

						result.Status = semantic.Status.ToString();
						result.Fields = new List<IndexFieldDisplay>
					{
						new IndexFieldDisplay
						{
							Name = "Qualifier",
							Label = "Qualifier",
							Type = "Text",
							Value = semantic.Qualifier
						},
						new IndexFieldDisplay
						{
							Name = "Threshold",
							Label = "Threshold",
							Type = "Text",
							Value = semantic.Threshold.ToString() + " %",
						},
						new IndexFieldDisplay
						{
							Name = "Priority",
							Label = "Priority",
							Type = "Text",
							Value = semantic.Priority.ToString()
						},
						new IndexFieldDisplay
						{
							Name = "BaseType",
							Label = "Base Type",
							Type = "Text",
							Value = semantic.BaseType.ToString()
						}
					};
					}
				}
			}			
		}

		#endregion

		/// <summary>
		/// Parses the new full-text search results into an Excel payload.
		/// </summary>
		private SLDocument SearchResultsAsExcel(List<SearchResult> results)
		{
			SLDocument document = new SLDocument();

			AddSearchResultsSheet(document, "Search Results", results, null);

			//List<Guid> assetTypeUidWithFields = GetAssetTypeUidWithField(results);
			//assetTypeUidWithFields.ForEach(assetTypeUid =>
			//{
			//	AssetType assetType = Company.AssetTypes.Where(a => a.uid == assetTypeUid).FirstOrDefault();
			//	var fieldTypes = Company.Filter<FieldType>(f => f.AssetTypeID == assetType.ID && f.SearchAddToResult).ToList();

			//	AddSearchResultsSheet(document, assetType.Name, model.Results.Where(r => r.AssetTypeUid == assetTypeUid), fieldTypes);
			//});
			document.DeleteWorksheet(SLDocument.DefaultFirstSheetName);

			return document;
		}

		private void AddSearchResultsSheet(SLDocument document, string name, List<SearchResult> results, IEnumerable<FieldType> fieldTypes)
		{
			document.AddWorksheet(name);
			int index = 1;
			int rownum = 1;
			document.SetCellValue(1, index++, "Category");
			document.SetCellValue(1, index++, "Type");
			document.SetCellValue(1, index++, "Name");
			document.SetCellValue(1, index++, "Status");
			document.SetCellValue(1, index++, "Data Quality Score");
			document.SetCellValue(1, index++, "Governance Score");
			document.SetCellValue(1, index++, "Asset Path");
			document.SetCellValue(1, index++, "Asset Type Path");
			document.SetCellValue(1, index++, "Tags");

			fieldTypes?.ToList().ForEach(ft =>
			{
				document.SetCellValue(1, index++, ft.FriendlyName.GetSafeXLSColumnValue());
			});

			document.SetCellValue(1, index++, "Asset UID");
			document.SetCellValue(1, index++, "Asset Type UID");
			document.SetCellValue(1, index++, "URL");

			foreach (var res in results)
			{
				rownum++;
				index = 1;
				document.SetCellValue(rownum, index++, res.Group.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Type.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Name.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == ScoreType.DataQuality) ? res.Scores.Where(s => s.ScoreType == ScoreType.DataQuality).Select(s => s.Value).FirstOrDefault().ToString() : null);
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == ScoreType.Governance) ? res.Scores.Where(s => s.ScoreType == ScoreType.Governance).Select(s => s.Value).FirstOrDefault().ToString() : null);
				//document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => string.Join(" / ", p.Key))));
				//document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => p.AssetType)));
				//document.SetCellValue(rownum, index++, res.Tags == null ? "" : string.Join("|", res.Tags?.Select(t => t.Value)).GetSafeXLSColumnValue());

				fieldTypes?.ToList().ForEach(ft =>
				{
					var field = res.Fields.Where(f => f.Name == ft.Name).FirstOrDefault();
					document.SetCellValue(rownum, index++, field?.Value.GetSafeXLSColumnValue());
				});

				document.SetCellValue(rownum, index++, res.Uid.ToString());
				document.SetCellValue(rownum, index++, res.AssetTypeUid.ToString());
				document.SetCellValue(rownum, index++, res.AbsoluteUrl);
			}

			for (int ci = 1; ci < index; ci++)
			{
				document.AutoFitColumn(ci);
			}
		}

		private SLDocument ResultsAsExcel(IndexResults model)
		{
			SLDocument document = new SLDocument();

			AddResultsSheet(document, "Search Results", model.Results, null);

			List<Guid> assetTypeUidWithFields = GetAssetTypeUidWithField(model.Results);
			assetTypeUidWithFields.ForEach(assetTypeUid =>
			{
				AssetType assetType = Company.AssetTypes.Where(a => a.uid == assetTypeUid).FirstOrDefault();
				var fieldTypes = Company.Filter<FieldType>(f => f.AssetTypeID == assetType.ID && f.SearchAddToResult).ToList();

				AddResultsSheet(document, assetType.Name, model.Results.Where(r => r.AssetTypeUid == assetTypeUid), fieldTypes);
			});
			document.DeleteWorksheet(SLDocument.DefaultFirstSheetName);

			return document;
		}

		private void AddResultsSheet(SLDocument document, string name, IEnumerable<IndexResult> results, IEnumerable<FieldType> fieldTypes)
		{
			document.AddWorksheet(name);
			int index = 1;
			int rownum = 1;
			document.SetCellValue(1, index++, "Category");
			document.SetCellValue(1, index++, "Type");
			document.SetCellValue(1, index++, "Name");
			document.SetCellValue(1, index++, "Status");
			document.SetCellValue(1, index++, "Data Quality Score");
			document.SetCellValue(1, index++, "Governance Score");
			document.SetCellValue(1, index++, "Asset Path");
			document.SetCellValue(1, index++, "Asset Type Path");
			document.SetCellValue(1, index++, "Tags");

			fieldTypes?.ToList().ForEach(ft =>
			{
				document.SetCellValue(1, index++, ft.FriendlyName.GetSafeXLSColumnValue());
			});

			document.SetCellValue(1, index++, "Asset UID");
			document.SetCellValue(1, index++, "Asset Type UID");
			document.SetCellValue(1, index++, "URL");

			foreach (IndexResult res in results)
			{
				rownum++;
				index = 1;
				document.SetCellValue(rownum, index++, res.Group.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.Type.GetSafeXLSColumnValue());
				document.SetCellValue(rownum, index++, res.DisplayName.GetSafeXLSColumnValue());
				string status = null;

				try
				{
					status = JsonConvert.DeserializeObject<dynamic>(res.Status)?[0].name;
				}
				catch
				{
					//On error, status=null will be the results
				}

				document.SetCellValue(rownum, index++, status);
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == "DataQuality") ? res.Scores.Where(s => s.ScoreType == "DataQuality").Select(s => s.Value).FirstOrDefault().ToString() : null);
				document.SetCellValue(rownum, index++, res.Scores.Exists(s => s.ScoreType == "Governance") ? res.Scores.Where(s => s.ScoreType == "Governance").Select(s => s.Value).FirstOrDefault().ToString() : null);
				document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => string.Join(" / ", p.Key))));
				document.SetCellValue(rownum, index++, res.AssetPath == null ? "" : string.Join(" > ", res.AssetPath.Select(p => p.AssetType)));
				document.SetCellValue(rownum, index++, res.Tags == null ? "" : string.Join("|", res.Tags?.Select(t => t.Value)).GetSafeXLSColumnValue());

				fieldTypes?.ToList().ForEach(ft =>
				{
					var field = res.Fields.Where(f => f.Name == ft.Name).FirstOrDefault();
					document.SetCellValue(rownum, index++, field?.Value.GetSafeXLSColumnValue());
				});

				document.SetCellValue(rownum, index++, res.Uid.ToString());
				document.SetCellValue(rownum, index++, res.AssetTypeUid.ToString());
				document.SetCellValue(rownum, index++, res.Url);
			}

			for (int ci = 1; ci < index; ci++)
			{
				document.AutoFitColumn(ci);
			}
		}

		private async Task<QueryLimitation> GetQueryLimitation()
		{
			List<string> blockedCategories = new List<string>();

			QueryLimitation limits = new QueryLimitation
			{
				IsAdministrator = SecurityContext.IsAdministrator,
				ResourceID = SecurityContext.ResourceID,
				ResourceGroupIDs = Company.ResourceGroups.Where(i => i.ResourceID == SecurityContext.ResourceID).Select(i => i.GroupID).ToList(),
			};

			if (SecurityContext.IsAdministrator)
			{
				limits.HideData3SixtyUsers = await GetHideData3SixtyUsers();
			}
			else
			{
				blockedCategories.Add(AssetTypeClass.User.ToString());
				blockedCategories.Add(AssetTypeClass.Group.ToString());
			}

			if (!await GetFeatureFlagValue(FlagList.SEMANTIC_TYPES_API))
			{
				blockedCategories.Add(AssetTypeClass.SemanticType.ToString());
			}

			//if (blockedCategories.Any())
			//{
			//	limits.AggregationFilters.Add(
			//		new AggregationFilter
			//		{ 
			//			Class = "Category",
			//			Uid = blockedCategories.ToArray()
			//		}
			//	);
			//}

			return limits;
		}

		private string SanitizeErrorMessage(Exception ex)
        {
			StringBuilder errorMessage = new StringBuilder();
			errorMessage.Append(ex.Message);

			if (ex.InnerException != null)
			{
				string innerMessage = Regex.Replace(ex.InnerException.Message, @"\/d3s\d+(\/_doc)?", "", RegexOptions.Multiline | RegexOptions.IgnoreCase);
				errorMessage.Append(" ");
				errorMessage.Append(innerMessage);
			}
			return errorMessage.ToString();
		}

		private struct SearchAugment : IEquatable<SearchAugment>
		{
			public SearchAugment(Guid guid, string status, string obj, long objectid, bool profile)
			{
				AssetUid = guid;
				Status = status;
				Object = obj;
				ObjectId = objectid;
				HasProfiling = profile;
			}

			public Guid AssetUid;
			public string Status;
			public string Object;
			public long ObjectId;
			public bool HasProfiling;

			public bool Equals(SearchAugment other)
			{
				return AssetUid.Equals(other.AssetUid);
			}
		}
	}
}
