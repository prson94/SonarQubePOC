using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.extensions.search;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Resources;
using SpreadsheetLight;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        ApiExplorerSettings(IgnoreApi = false)
    ]
    public class SearchController : BaseV2ApiController
    {
        private readonly ISearchSource SearchSource;
        private readonly IAssetRepository AssetRepository;

        public SearchController(ICoreComponentSet set, ISearchSource searchSource, IAssetRepository repository) : base(set)
        {
            SearchSource = searchSource;
            AssetRepository = repository;
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
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IQueryable<IndexResult> GetSearchResults(string phrase)
        {
            if (!string.IsNullOrEmpty(phrase))
            {
                var result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, phrase, 200, 0);
                result.Results.ForEach(i => {
                    i.AbsoluteUrl = string.Format($"https://{Community.GetPrimaryUrlPrefix()}.data3sixty.com/{i.Url}");
                });
                return result.Results.AsQueryable();
            }
            return null;            
        }

        /// <summary>
        /// Global Search
        /// </summary>
        /// <param name="queryRequest">Search Query Request</param>
        /// <returns></returns>
        [
            HttpPost,
            Route("results"),
            SwaggerConsumes("application/json"),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.OK, "Search results matching the query.", typeof(SearchResultsViewModel)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetResultsAsync(QueryRequest queryRequest)
        {
            try
            {
                var isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
                var o = new SearchResultsViewModel();

                if (!string.IsNullOrEmpty(queryRequest.Term))
                {
                    if(queryRequest.Size > 5000)
                    {
                        queryRequest.Size = 5000;
                    }

                    //Convert Tag filters to Tag UID filters
                    queryRequest.FieldFilters.Where(f => f.Field == "Tags").ToList().ForEach(f => {
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

                    o.Result = SearchSource.GetSearchResultsWithAggregation(Company.CurrentCompanyID, Company.CurrentResourceID, queryRequest, o.Categories, GetQueryLimitation());

                    await AugmentResults(o.Result.Results).ConfigureAwait(false);
                }

                HttpResponseMessage response;

                if (isStreamResponse)
                {
                    SLDocument document = ResultsAsExcel(o);
                    // Select the first worksheet as the active one.
                    var firstSheet = document.GetWorksheetNames()[0];
                    document.SelectWorksheet(firstSheet);

                    var stream = new MemoryStream();
                    document.SaveAs(stream);

                    response = createFileResponseMessage(HttpStatusCode.OK, "SearchResults.xlsx", stream.ToArray());
                }
                else
                {
                    response = Request.CreateResponse(HttpStatusCode.OK, o);
                }
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Typeahead search suggestions.
        /// </summary>
        /// <param name="q">Query string</param>
        /// <param name="t">Comma separated list of Categories to limit search to</param>
        /// <param name="num">Max number of results. Defaults to 7</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("typeahead"),
            SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Search result suggestions based on query.", typeof(IList<TypeaheadResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetTypeaheads(string q, string t = null, int? num = null)
        {
            try
            {
                IList<TypeaheadResult> res = null;
                if (!string.IsNullOrEmpty(q))
                {
                    res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, Company.CurrentResourceID, q, GetQueryLimitation(), num.GetValueOrDefault(7), t).ToList();
                    await AugmentResults(res).ConfigureAwait(false);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, res))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, ApiMessages.UnknownError, errorMessage)).ConfigureAwait(false);
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
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> GetCategories()
        {
            List<string> visibleCategories = assetTypeClasses.Where(c => Company.AssetTypes.Any(at => at.Class == c)).Select(c => c.ToString()).ToList();

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
            SwaggerResponse(HttpStatusCode.OK, "Search index count aggregated by Category", typeof(IndexResult)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetStatus()
        {
            var o = new SearchResultsViewModel();
            o.Result = SearchSource.GetStatusSearch(Company.CurrentCompanyID, o.Categories, true);
            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, o));
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
            List<IndexableStatus> status = Company.Query<IndexableStatus>("SELECT Class, AssetTypeUid, Status, TargetCount, Start, LastUpdate FROM [queue].[Search] WHERE Active = 1").ToList();
            status.ForEach((t) => t.ClassName = SearchIndexer.GetCategoryFromClass(t.Class));

            List<IndexableCount> esStatus = SearchSource.GetStatusList(Company.CurrentCompanyID);
            esStatus.ForEach((es) => {
                es.Class = SearchIndexer.GetClassFromCategory(es.ClassName);
                IndexableStatus st = status.Find((s) => s.Class == es.Class && s.AssetTypeUid == es.AssetTypeUid);
                if (st != null)
                {
                    st.CurrentCount = es.CurrentCount;
                }
                else
                {
                    status.Add(new IndexableStatus
                    {
                        Class = es.Class,
                        ClassName = es.ClassName,
                        AssetTypeUid = es.AssetTypeUid,
                        CurrentCount = es.CurrentCount,
                        Status = 0
                    });
                }
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
            Route("rebuild/{Class:int}/{assetTypeUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "Creates a new Bulk load.", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error indicating the user does not have permission to perform this action.", typeof(ErrorResponse)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> DoRebuild(int Class, Guid assetTypeUid)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Forbidden, ApiMessages.InvalidRequest, ApiMessages.EndpointNotAuthorizedMessage)).ConfigureAwait(false);
            }
            var response = new ConfirmResponse();

            SearchIndexer indexer = new SearchIndexer(Company.Connection, Company.CurrentCompanyID, SearchSource);
            indexer.QueueRebuildRequest((AssetTypeClass)Class, assetTypeUid);

            response.message = "Rebuild queued";

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, response))).ConfigureAwait(false);
        }

        #region Enrich elastic results with DB data

        private readonly static List<AssetTypeClass> assetTypeClasses = new List<AssetTypeClass> {
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

        //Icons set based on Category/Class directly
        private static readonly Dictionary<string, string> categoryMap = new Dictionary<string, string> {
            { "User", "fa-user" },
            { "Group", "fa-users" },
            { "Grammatic Type", "fa-comments" },
            { "Attribute", "fa-pencil-square-o" },
            { "Diagram Asset", "fa-share-alt" }
        };

        //Icons set based on main Nav item for category
        private static readonly Dictionary<string, string> siteNavMap = new Dictionary<string, string> {
            { "Business Asset", "#Business" },
            { "Technical Asset", "#Technical" },
            { "Model", "#Models" },
            { "Reference", "#Reference" },
            { "Rule", "#Data Quality" },
            { "Policy", "#Policy" }
        };

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
                var sql = $@"WITH cteParents(AssetTypeUid, Object, ObjectID, Subject, SubjectID, Level)
                    AS (SELECT at.Uid as AssetTypeUid, it.Object, it.ObjectID, it.Subject, it.SubjectID, 1
                        FROM [dbo].[IntersectType] it 
                        INNER JOIN [dbo].[Predicate] p ON p.ID = it.PredicateID AND p.Type = 3
                        INNER JOIN [dbo].[AssetType] at ON at.Object = it.Object AND at.ObjectID = it.ObjectID
                        inner join @uids U on U.Uid = AT.Uid
                        UNION ALL
                        SELECT cteParents.AssetTypeUid, cteParents.Object, cteParents.ObjectID, it.Subject, it.SubjectID, cteParents.Level+1
                            FROM cteParents
                            INNER JOIN [dbo].[IntersectType] it ON it.Object = cteParents.Subject and it.ObjectID = cteParents.SubjectID
                            INNER JOIN [dbo].[Predicate] p ON P.ID = it.PredicateID and p.Type = 3)
                    SELECT cteParents.AssetTypeUid, nav.Icon, nav.ImageIconUrl
                    FROM cteParents
                    INNER JOIN SiteNav nav1 on cteParents.Subject = nav1.Object and cteParents.SubjectID = nav1.ObjectID
                    INNER JOIN SiteNav nav on nav1.ParentID = nav.ID
                    UNION ALL
                    SELECT at.Uid as AssetTypeUid, nav.Icon, nav.ImageIconUrl
                    FROM [dbo].[AssetType] at
                    INNER JOIN SiteNav nav1 on at.Object = nav1.Object and at.ObjectID = nav1.ObjectID
                    INNER JOIN SiteNav nav on nav1.ParentID = nav.ID
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
            await AugmentResults(results as IEnumerable<TypeaheadResult>).ConfigureAwait(false);

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
        }
        #endregion

        private SLDocument ResultsAsExcel(SearchResultsViewModel model)
        {
            SLDocument document = new SLDocument();

            AddResultsSheet(document, "Search Results", model.Result.Results, null);

            List<Guid> assetTypeUidWithFields = GetAssetTypeUidWithField(model.Result.Results);
            assetTypeUidWithFields.ForEach(assetTypeUid => {
                AssetType assetType = Company.AssetTypes.Where(a => a.uid == assetTypeUid).FirstOrDefault();
                var fieldTypes = Company.Filter<FieldType>(f => f.AssetTypeID == assetType.ID && f.SearchAddToResult).ToList();

                AddResultsSheet(document, assetType.Name, model.Result.Results.Where(r => r.AssetTypeUid == assetTypeUid), fieldTypes);
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

            fieldTypes?.ToList().ForEach(ft => {
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

                fieldTypes?.ToList().ForEach(ft => {
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
            QueryLimitation limits = new QueryLimitation
            {
                ResourceID = Company.CurrentResourceID,
                ResourceGroupIDs = Company.ResourceGroups.Where(i => i.ResourceID == Company.CurrentResourceID).Select(i => i.GroupID).ToList(),
                ResourceOrgIDs = Company.OrganizationResources.Where(r => r.ResourceID == Company.CurrentResourceID && (r.Accepted ?? false)).Select(r => r.OrganizationID).ToList()
            };
            if (Company.CurrentResourceIsAdmin)
            {
                limits.HideData3SixtyUsers = SettingsRepository.GetSettingValue<bool>(Setting.HideData3SixtyUsers);
            }
            else
            {
                limits.AggregationFilters.Add(
                    new AggregationFilter
                    {
                        Field = "d3sCategory",
                        Values = new string[] { AssetTypeClass.User.ToString(), AssetTypeClass.Group.ToString() }
                    }
                );
            }
            return limits;
        }

        struct SearchAugment : IEquatable<SearchAugment>
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
