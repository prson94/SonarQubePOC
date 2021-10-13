using d360.core;
using d360.core.enums;
using d360.extensions;
using d360.extensions.search;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Models;
using d360.web.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("search")]
    public class SearchController : BaseController
    {
        #region DI

        ISearchSource SearchSource;
        IAssetRepository AssetRepository;

        public SearchController(
            ICommunityContext community,
            ICompanyContext company,
            ISearchSource searchSource,
            IAssetRepository repository, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
            SearchSource = searchSource;
            AssetRepository = repository;
        }

        #endregion

        #region Json

        [HttpPost, Route("Results"), NonNullableParameters]
        public async Task<JsonResult> Results(QueryRequest queryRequest)
        {
            try
            {
                var o = new SearchResultsViewModel();

                if (!string.IsNullOrEmpty(queryRequest.Term))
                {
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
                    await AugmentResults(o.Result.Results);
                }
                return Json(o);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;
                return jsonException(ex, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet, Route("AutoComplete"), NonNullableParameters]
        public JsonResult AutoComplete(string search)
        {
            List<string> results = new List<string>();

            if (!string.IsNullOrEmpty(search))
            {
                results = SearchSource.GetSearchPhrases(Company.CurrentCompanyID, string.Format("{0}*",search), 20).ToList();                
            }

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("Typeahead"), NonNullableParameters]
        [ValidateInput(false)]
        public async Task<JsonNetResult> Typeahead(string q, string t, int? num)
        {
            try
            {
                if (!string.IsNullOrEmpty(q))
                {
                    IList<TypeaheadResult> res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, Company.CurrentResourceID, q, GetQueryLimitation(), num.GetValueOrDefault(7), t).ToList();
                    await AugmentResults(res);
                    return new JsonNetResult { Data = res, Formatting = Newtonsoft.Json.Formatting.None };
                }
                return new JsonNetResult { Data = null };
            }
            catch (System.Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [HttpGet, Route("Status")]
        public JsonResult Status()
        {
            var o = new SearchResultsViewModel();
            o.Result = SearchSource.GetStatusSearch(Company.CurrentCompanyID, o.Categories, true);
            return Json(o, JsonRequestBehavior.AllowGet);
        }

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

        [HttpGet, Route("Categories")]
        public JsonResult GetCategories()
        {
            List<string> visibleCategories = assetTypeClasses.Where(c => Company.AssetTypes.Any(at => at.Class == c)).Select(c => c.ToString()).ToList();

            //We have Grammatic Types if we have Nyms or any intersects with predicate type 6
            if (Company.Nyms.Any())
            {
                visibleCategories.Add("Synonym");
            }
            else if (Company.Query<int>(@"select case when exists(select *
                    from[intersect] I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6) then 1
                    else 0 end").FirstOrDefault() == 1)
            {
                visibleCategories.Add("Synonym");
            }

            return Json(visibleCategories, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("IndexableTypes")]
        public JsonResult GetIndexableTypes()
        {
            if(!Company.CurrentResourceIsAdmin)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }

            List<IndexableType> types = Company.Query<IndexableType>("SELECT Name, Class, Uid as AssetTypeUid FROM [dbo].[AssetType] at WHERE EXISTS (SELECT 1 FROM [dbo].[Asset] a WHERE a.AssetTypeId = at.ID)").ToList();
            types.ForEach((t) => t.ClassName = SearchIndexer.GetCategoryFromClass(t.Class));

            List<IndexableType> classes = assetTypeClasses.Where(c => types.Any(at => at.Class == (int)c)).Select(c => new IndexableType { Name = c.ToString(), Class = (int)c, AssetTypeUid = Guid.Empty, ClassName = c.ToString() }).ToList();
            
            //Overload "Predicate" class as a representation for synonyms
            classes.Add(new IndexableType { Name = "Synonym", Class = (int)AssetTypeClass.Predicate, AssetTypeUid = Guid.Empty, ClassName = AssetTypeClass.Predicate.ToString() });

            classes.AddRange(types);

            //Reclassify Reference/ReferenceItemType
            classes.Where((c) => c.Class == 9).ToList().ForEach((c) => c.Class = 14);

            return Json(classes, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("IndexableStatus")]
        public JsonResult GetIndexableStatus()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return Json(new { }, JsonRequestBehavior.AllowGet);
            }

            List<IndexableStatus> status = Company.Query<IndexableStatus>("SELECT Class, AssetTypeUid, Status, TargetCount, Start, LastUpdate FROM [queue].[Search] WHERE Active = 1").ToList();
            status.ForEach((t) => t.ClassName = SearchIndexer.GetCategoryFromClass(t.Class));

            List<IndexableCount> esStatus = SearchSource.GetStatusList(Company.CurrentCompanyID);
            esStatus.ForEach((es) => {
                es.Class = SearchIndexer.GetClassFromCategory(es.ClassName);
                IndexableStatus st = status.Find((s) => s.Class == es.Class && s.AssetTypeUid == es.AssetTypeUid);
                if(st != null)
                {
                    st.CurrentCount = es.CurrentCount;
                } else
                {
                    status.Add(new IndexableStatus {
                        Class = es.Class,
                        ClassName = es.ClassName,
                        AssetTypeUid = es.AssetTypeUid,
                        CurrentCount = es.CurrentCount,
                        Status = 0
                    });
                }
            });
            return Json(status, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, Route("rebuild/{Class:int}/{assetTypeUid:Guid}")]
        public JsonResult DoRebuild(int Class, Guid assetTypeUid)
        {
            if(!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return jsonException("User not authorized to perfom this action", System.Net.HttpStatusCode.Forbidden);
            }

            SearchIndexer indexer = new SearchIndexer(Company.Connection, Company.CurrentCompanyID, SearchSource);
            indexer.QueueRebuildRequest((AssetTypeClass)Class, assetTypeUid);
            return Json(new { }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Enrich elastic results with DB data

        //Icons set based on Category/Class directly
        private static readonly Dictionary<string, string> categoryMap = new Dictionary<string, string>() {
            { "User", "fa-user" },
            { "Group", "fa-users" },
            { "Grammatic Type", "fa-comments" },
            { "Attribute", "fa-pencil-square-o" },
            { "Diagram Asset", "fa-share-alt" }
        };

        //Icons set based on main Nav item for category
        private static readonly Dictionary<string, string> siteNavMap = new Dictionary<string, string>() {
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
                            new List<string>() { "Uid" })
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
            if (results.Where(r => r.MissingIcon() && r.AssetTypeUid != null).Any())
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
                            new List<string>() { "Uid" })
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
            foreach(var r in results.Where(r => r.Uid.HasValue))
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
            await AppendIcons(results);
            await AppendPaths(results);
        }

        private async Task AugmentResults(IEnumerable<IndexResult> results)
        {
            await AugmentResults(results as IEnumerable<TypeaheadResult>);

            if(!results.Any())
            {
                return;
            }

            //Determine which results have asset tyoes with search fields defined
            List<Guid> assetTypeUidWithFields = Company.Query<Guid>(
                @"SELECT at.uid
                FROM assettype at
                INNER JOIN @uids U on U.Uid = AT.Uid
                WHERE exists (select 1 from fieldtype ft where ft.AssetTypeID = at.id and ft.SearchAddToResult = 1)", new
                {
                    uids = results.Where(r => r.AssetTypeUid != null).Select(r => r.AssetTypeUid.ToString()).Distinct().AsTableValuedParameter(
                        "dbo.UidTable",
                        new List<string>() { "Uid" })
                })
                .ToList();

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
                new {
                    uids = results
                            .Where(r => r.Uid != null)
                            .Select(r => r.Uid.ToString())
                            .Distinct()
                            .AsTableValuedParameter( "dbo.UidTable", new List<string> { "Uid" })
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
                if(augment.AssetUid != Guid.Empty)
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
                        Values = new string[] { "Resource", "Group" }
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
