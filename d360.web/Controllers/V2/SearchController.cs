using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.extensions;
using d360.extensions.search;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;

using Microsoft.ApplicationInsights;
using Microsoft.Web.Http;

using Newtonsoft.Json;

using Resources;

using SpreadsheetLight;

using Swashbuckle.Swagger.Annotations;

namespace d360.web.Controllers.V2
{
	/// <summary>
	/// This service houses all endpoints handling search in Govern.
	/// </summary>
	[
		ApiVersion("2.0"),
		RoutePrefix("api/v{version:apiVersion}/search"),
		Authorize,
		ApiExplorerSettings(IgnoreApi = false)
	]
	public class SearchController : BaseV2ApiController
	{
		private readonly ISearchSource SearchSource;
		private readonly IAssetRepository AssetRepository;
		private readonly ISemanticsRepository SemanticsRepository;
		private readonly TelemetryClient Telemetry;

		//Icons set based on main Nav item for category
		private readonly Dictionary<string, string> siteNavMap;

		public SearchController(ICoreComponentSet set, ISearchSource searchSource, IAssetRepository assetRepository, ISemanticsRepository semanticsRepository) : base(set)
		{
			SearchSource = searchSource;
			AssetRepository = assetRepository;
			SemanticsRepository = semanticsRepository;
			Telemetry = new TelemetryClient();

			this.siteNavMap = new Dictionary<string, string> {
				{ CommonNames.AssetTypeClass_Business, "#Business" },
				{ CommonNames.AssetTypeClass_Technical, "#Technical" },
				{ CommonNames.AssetTypeClass_Model, "#Models" },
				{ CommonNames.AssetTypeClass_Reference, "#Reference" },
				{ CommonNames.AssetTypeClass_Rule, "#Data Quality" },
				{ CommonNames.AssetTypeClass_Policy, "#Policy" },
				{ CommonNames.AssetTypeClass_SemanticType, "#SemanticTypes" }
			};
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
		public IQueryable<IndexResult> GetSearchResults(string phrase)
		{
			if (!string.IsNullOrEmpty(phrase))
			{
				var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, phrase, 200, 0, GetQueryLimitation());
				result.Results.ForEach(i =>
				{
					i.AbsoluteUrl = string.Format($"https://{Community.GetPrimaryUrlPrefix()}.data3sixty.com/{i.Url}");
				});

				return result.Results.AsQueryable();
			}

			return null;
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
		public async Task<IHttpActionResult> GetResultsAsync(QueryRequest queryRequest)
		{
			try
			{
				var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
				var resultset = new IndexResults();
				string isValid = ValidateQueryRequest(queryRequest);

				if (!string.IsNullOrEmpty(isValid))
				{
					return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
				}

				if (!string.IsNullOrEmpty(queryRequest.Term))
				{
					//Convert Tag filters to Tag UID filters
					queryRequest.FieldFilters.Where(f => f.Field == "Tags").ToList().ForEach(f =>
					{
						FieldFilter taguids = new FieldFilter
						{
							Field = "TagUids",
							Connector = f.Connector,
							Operator = f.Operator,
							MatchWords = f.MatchWords,
							Values = f.Values.Select(v => Company.Tags.FirstOrDefault(t => t.Value == v).uid.ToString()).ToArray()
						};
						queryRequest.FieldFilters.Add(taguids);
					});

					queryRequest.FieldFilters.RemoveAll(f => f.Field == "Tags");
					queryRequest.FieldBoosters = Company.Query<FieldBoost>("SELECT Field, Boost FROM [dbo].[SearchBoost]").ToList();
					resultset = SearchSource.GetSearchResultsWithAggregation(Company.CurrentCompanyID, queryRequest, GetQueryLimitation());

					int augmentTime = 0;

					if (resultset.Results.Any())
					{
						Stopwatch timer = new Stopwatch();
						timer.Start();

						await AugmentResults(resultset.Results).ConfigureAwait(false);

						timer.Stop();
						augmentTime = Convert.ToInt32(Math.Round(timer.Elapsed.TotalMilliseconds));
					}

					resultset.ElapsedMS.Add("Augment", augmentTime);
				}

				HttpResponseMessage response;

				if (isStreamResponse)
				{
					SLDocument document = ResultsAsExcel(resultset);
					// Select the first worksheet as the active one.
					var firstSheet = document.GetWorksheetNames()[0];
					document.SelectWorksheet(firstSheet);

					var stream = new MemoryStream();
					document.SaveAs(stream);

					response = createFileResponseMessage(HttpStatusCode.OK, "SearchResults.xlsx", stream.ToArray());
				}
				else
				{
					response = Request.CreateResponse(HttpStatusCode.OK, resultset);
				}

				return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
			}
			catch (SearchServerConnectionException ex)
			{
				Telemetry.TrackException(ex);

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, SearchApiMessages.NoSearchServerConnection, ex.Message)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, SanitizeErrorMessage(ex))).ConfigureAwait(false);
			}
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
			try
			{
				if (!string.IsNullOrWhiteSpace(categories))
				{
					IEnumerable<string> categoryList = categories.Split(',').Select(c => c.Trim());
					IEnumerable<string> invalidCategories = categoryList.Except(GetVisibleCategories());

					if (invalidCategories.Any())
					{
						return await Task.FromResult(errorMessageResponse(
							HttpStatusCode.BadRequest,
							ApiMessages.InvalidRequest,
							string.Format(SearchApiMessages.CategoryNotAvailable, string.Join(", ", invalidCategories))
						)).ConfigureAwait(false);
					}
				}

				IList<TypeaheadResult> res = null;
				if (!string.IsNullOrEmpty(query))
				{
					res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, query, GetQueryLimitation(), num.GetValueOrDefault(7), categories).ToList();
					await AugmentResults(res).ConfigureAwait(false);
				}

				return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res))).ConfigureAwait(false);
			}
			catch (SearchServerConnectionException ex)
			{
				Telemetry.TrackException(ex);

				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, SearchApiMessages.NoSearchServerConnection, ex.Message)).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, SanitizeErrorMessage(ex))).ConfigureAwait(false);
			}
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
			List<string> visibleCategories = GetVisibleCategories();

			return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, visibleCategories))).ConfigureAwait(false);
		}

		/// <summary>
		/// Returns search index count by category
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("status"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "Search index count aggregated by Category", typeof(IndexResults)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public IHttpActionResult GetStatus()
		{
			var resultset = SearchSource.GetStatusSearch(Company.CurrentCompanyID, true);

			return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, resultset));
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
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetIndexableTypes()
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.InvalidRequest, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
			}

			List<IndexableType> types = Company.Query<IndexableType>("SELECT Name, Class, Uid as AssetTypeUid FROM [dbo].[AssetType] at WHERE EXISTS (SELECT 1 FROM [dbo].[Asset] a WHERE a.AssetTypeId = at.ID)").ToList();
			types.ForEach((t) => t.ClassName = SearchIndexer.GetCategoryFromClass(t.Class));

			List<IndexableType> classes = assetTypeClasses.Where(c => types.Any(at => at.Class == (int)c)).Select(c => new IndexableType { Name = c.ToString(), Class = (int)c, AssetTypeUid = Guid.Empty, ClassName = c.ToString() }).ToList();

			if (GetBoolFlag(FeatureFlags.PERM_SEMANTIC_TYPES_API))
			{
				classes.Add(new IndexableType { Name = AssetTypeClass.SemanticType.ToString(), Class = (int)AssetTypeClass.SemanticType, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.SemanticType.ToString() });
			}

			//Overload "Predicate" class as a representation for synonyms
			classes.Add(new IndexableType { Name = "Synonym", Class = (int)AssetTypeClass.Predicate, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.Predicate.ToString() });

			classes.AddRange(types);

			//Reclassify Reference/ReferenceItemType
			classes.Where((c) => c.Class == 9).ToList().ForEach((c) => c.Class = 14);

			return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, classes))).ConfigureAwait(false);
		}

		/// <summary>
		/// Lists counts of items in search index by Category and AssetType
		/// </summary>
		/// <returns></returns>
		[
			HttpGet,
			Route("indexableStatus"),
			SwaggerProduces("application/json"),
			SwaggerResponse(HttpStatusCode.OK, "List of indexable Categories and Asset Types", typeof(List<IndexableStatus>)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> GetIndexableStatus()
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.InvalidRequest, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
			}

			List<IndexableCount> dbCounts = GetDatabaseCounts();
			List<IndexableCount> esStatus = SearchSource.GetStatusList(Company.CurrentCompanyID);
			List<IndexableStatus> queueStatus = Company.Query<IndexableStatus>("SELECT Class, AssetTypeUid, Status, TargetCount, Start, LastUpdate FROM [queue].[Search] WHERE Active = 1").ToList();

			IEnumerable<IndexableStatus> status = dbCounts.Select(db => {
				var res = new IndexableStatus
				{
					Class = db.Class,
					ClassName = SearchIndexer.GetCategoryFromClass(db.Class),
					AssetTypeUid = db.AssetTypeUid,
					DatabaseCount = db.CurrentCount,
					Status = 0
				};

				var st = queueStatus.Find((s) => s.Class == db.Class && s.AssetTypeUid == db.AssetTypeUid);
				if(st != null)
				{
					res.TargetCount = st.TargetCount;
					res.Start = st.Start;
					res.LastUpdate = st.LastUpdate;
					res.Status = st.Status;
				}

				var es = esStatus.Find((s) => s.ClassName == res.ClassName && s.AssetTypeUid == res.AssetTypeUid);
				if (es != null)
				{
					res.CurrentCount = es.CurrentCount;
				}

				if(res.Class == (int)AssetTypeClass.Reference)
				{
					res.Class = (int)AssetTypeClass.ReferenceItemType;
				}

				return res;
			});

			return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, status))).ConfigureAwait(false);
		}

		/// <summary>
		/// Rebuild search index for Category or Asset Type
		/// </summary>
		/// <param name="Class">Category class id</param>
		/// <param name="assetTypeUid">Asset Type UID</param>
		/// <returns></returns>
		[
			HttpPost,
			Route("rebuild"),
			SwaggerResponse(HttpStatusCode.OK, "Queues a rebuild request.", typeof(ConfirmResponse)),
			SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
			ApiExplorerSettings(IgnoreApi = true)
		]
		public async Task<IHttpActionResult> DoRebuild(List<SearchPartialRebuildRequest> rebuildRequests)
		{
			if (!Company.CurrentResourceIsAdmin)
			{
				return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.InvalidRequest, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
			}

			var response = new ConfirmResponse();
			SearchIndexer indexer = new SearchIndexer(Company.Connection, Company.CurrentCompanyID, SearchSource);
			rebuildRequests.ForEach(r => {
				indexer.QueueRebuildRequest((AssetTypeClass)r.Class, r.AssetTypeUid);
			});
			response.message = "Rebuild queued";

			return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);
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
			"Tags"
		};

		//Icons set based on Category/Class directly
		private static readonly Dictionary<string, string> categoryMap = new Dictionary<string, string> {
			{ "User", "fa-user" },
			{ "Group", "fa-users" },
			{ "Grammatic Type", "fa-comments" },
			{ "Attribute", "fa-pencil-square-o" },
			{ "Diagram Asset", "fa-share-alt" }
		};

		private List<string> GetVisibleCategories()
		{
			List<string> visibleCategories = assetTypeClasses.Where(c => Company.AssetTypes.Any(at => at.Class == c)).Select(c => c.ToString()).ToList();

			if (Company.Semantics.Any() && GetBoolFlag(FeatureFlags.PERM_SEMANTIC_TYPES_API))
			{
				visibleCategories.Add(AssetTypeClass.SemanticType.ToString());
			}

			//We have Grammatic Types if we have Nyms or any intersects with predicate type 6
			if (Company.Nyms.Any())
			{
				visibleCategories.Add("Synonym");
			}
			else if (Company.Query<int>(@"select case when exists(select *
					from [intersect] I
					inner join IntersectType T on T.ID = I.IntersectTypeID
					inner join Predicate P on P.ID = T.PredicateID and P.Type = 6) then 1
					else 0 end").FirstOrDefault() == 1)
			{
				visibleCategories.Add("Synonym");
			}

			return visibleCategories;
		}

		private List<IndexableCount> GetDatabaseCounts()
		{
			var semanticTypesEnabled = GetBoolFlag(FeatureFlags.PERM_SEMANTIC_TYPES_API);

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

		private string ValidateQueryRequest(QueryRequest queryRequest)
		{
			if (queryRequest.Size > 5000)
			{
				return string.Format(SearchApiMessages.SizeTooBig, 5000);
			}

			if (queryRequest.Aggregations.Any())
			{
				IEnumerable<string> unsupportedAggregations = queryRequest.Aggregations.Except(supportedAggregations);

				if (unsupportedAggregations.Any())
				{
					return string.Format(SearchApiMessages.AggregationUnsupported, string.Join(", ", unsupportedAggregations));
				}
			}

			if (queryRequest.AggregationFilters.Any())
			{
				IEnumerable<string> aggFilters = queryRequest.AggregationFilters.Select(f => f.Field);
				IEnumerable<string> unsupportedAggFilters = aggFilters.Except(supportedAggregationFilters);

				if (unsupportedAggFilters.Any())
				{
					return string.Format(SearchApiMessages.AggregationFilterUnsupported, string.Join(", ", unsupportedAggFilters));
				}

				if (queryRequest.AggregationFilters.Exists(f => f.Field == "Category"))
				{
					IEnumerable<string> categoryList = queryRequest.AggregationFilters.Where(f => f.Field == "Category").FirstOrDefault().Values;
					IEnumerable<string> invalidCategories = categoryList.Except(GetVisibleCategories());

					if (invalidCategories.Any())
					{
						return string.Format(SearchApiMessages.CategoryNotAvailable, string.Join(", ", invalidCategories));
					}
				}
			}

			if (queryRequest.FieldFilters.Any())
			{
				IEnumerable<string> fieldFilters = queryRequest.FieldFilters.Select(f => f.Field);
				IEnumerable<string> unsupportedFields = fieldFilters.Except(supportedFields);

				if (unsupportedFields.Any())
				{
					return string.Format(SearchApiMessages.FieldUnsupported, string.Join(", ", unsupportedFields));
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

				foreach (var m in menuItems)
				{
					foreach (var r in results.Where(r => r.AssetTypeUid == m.AssetTypeUid))
					{
						if (!string.IsNullOrEmpty(m.ImageIconUrl))
						{
							r.ImageUrl = constants.COMPANY_RESOURCES_URL + m.ImageIconUrl;
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

				foreach (var r in results.Where(res => res.MissingIcon() && iconMap.ContainsKey(siteNavMap[res.Group])))
				{
					if (!string.IsNullOrEmpty(iconMap[siteNavMap[r.Group]].Item2))
					{
						r.ImageUrl = constants.COMPANY_RESOURCES_URL + iconMap[siteNavMap[r.Group]].Item2;
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

			// FeatureFlags
			var dataProfilingEnabled = GetBoolFlag(FeatureFlags.PERM_DATA_PROFILING);
			var semanticTypesEnabled = GetBoolFlag(FeatureFlags.PERM_SEMANTIC_TYPES_API);

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
					result.HasProfiling = dataProfilingEnabled ? augment.HasProfiling : false;
				}

				result.Scores = searchScores.Where(s => s.AssetUid == result.Uid).ToList();
			}

			if (semanticTypesEnabled)
			{
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
		}

		#endregion

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
				document.SetCellValue(1, index++, ft.FriendlyName);
			});

			document.SetCellValue(1, index++, "Asset UID");
			document.SetCellValue(1, index++, "Asset Type UID");
			document.SetCellValue(1, index++, "URL");

			foreach (IndexResult res in results)
			{
				rownum++;
				index = 1;
				document.SetCellValue(rownum, index++, res.Group);
				document.SetCellValue(rownum, index++, res.Type);
				document.SetCellValue(rownum, index++, res.DisplayName);
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
				document.SetCellValue(rownum, index++, res.Tags == null ? "" : string.Join("|", res.Tags?.Select(t => t.Value)));

				fieldTypes?.ToList().ForEach(ft =>
				{
					var field = res.Fields.Where(f => f.Name == ft.Name).FirstOrDefault();
					document.SetCellValue(rownum, index++, field?.Value);
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

		private QueryLimitation GetQueryLimitation()
		{
			List<string> blockedCategories = new List<string>();

			QueryLimitation limits = new QueryLimitation
			{
				ResourceID = Company.CurrentResourceID,
				ResourceGroupIDs = Company.ResourceGroups.Where(i => i.ResourceID == Company.CurrentResourceID).Select(i => i.GroupID).ToList(),
			};

			if (Company.CurrentResourceIsAdmin)
			{
				limits.HideData3SixtyUsers = SettingsRepository.GetSettingValue<bool>(Setting.HideData3SixtyUsers);
			}
			else
			{
				blockedCategories.Add(AssetTypeClass.User.ToString());
				blockedCategories.Add(AssetTypeClass.Group.ToString());
			}

			if (!GetBoolFlag(FeatureFlags.PERM_SEMANTIC_TYPES_API))
			{
				blockedCategories.Add(AssetTypeClass.SemanticType.ToString());
			}

			if (blockedCategories.Any())
			{
				limits.AggregationFilters.Add(
					new AggregationFilter
					{
						Field = "Category",
						Values = blockedCategories.ToArray()
					}
				);
			}

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
