using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using d360.model.DataAccessLayer;
using d360.web.Models;
using d360.web.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            IAssetRepository repository)
            : base(community, company)
        {
            SearchSource = searchSource;
            AssetRepository = repository;
        }

        #endregion

        #region Json

        [HttpPost, Route("Results"), NonNullableParameters]
        public async Task<JsonResult> Results(QueryRequest queryRequest)
        {
            var o = new SearchResultsViewModel();

            if (!string.IsNullOrEmpty(queryRequest.Term))
            {
                queryRequest.FieldBoosters = Company.Query<FieldBoost>("SELECT Field, Boost FROM [dbo].[SearchBoost]").ToList();
                o.Result = SearchSource.GetSearchResultsWithAggregation(Company.CurrentCompanyID, Company.CurrentResourceID, queryRequest, o.Categories, GetQueryLimitation());
                foreach(var r in o.Result.Results)
                {
                    await AugmentResult(r);
                }
            }
            return Json(o);
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
                    foreach(TypeaheadResult result in res)
                    {
                        await AugmentResult(result);
                    }

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
            o.Result = SearchSource.GetStatusSearch(Company.CurrentCompanyID, o.Categories);
            return Json(o, JsonRequestBehavior.AllowGet);
        }

        private readonly static List<AssetTypeClass> assetTypeClasses = new List<AssetTypeClass> {
            AssetTypeClass.BusinessAsset,
            AssetTypeClass.TechnicalAsset,
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
                visibleCategories.Add("Synonym");
            else if (Company.Query<int>(@"select case when exists(select *
                    from[intersect] I
                    inner join IntersectType T on T.ID = I.IntersectTypeID
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6) then 1
                    else 0 end").FirstOrDefault() == 1)
                visibleCategories.Add("Synonym");

            if (Community.IsFusionEnabled())
            {
                if (Company.FusionAttributes.Any())
                    visibleCategories.Add("FusionAttributes");

                if (Company.FusionTypes.Any())
                    visibleCategories.Add("FusionType");
            }

            return Json(visibleCategories, JsonRequestBehavior.AllowGet);
        }

        #endregion

        private AssetTypeStyle GetAssetTypeStyle(Guid? AssetTypeUid)
        {
            if (AssetTypeUid.HasValue && AssetTypeUid != Guid.Empty)
            {
                var style = Company.GetAssetTypeStyle(AssetTypeUid.Value);
                return style;
            }
            return null;
        }
        private string GetIcon(Guid? AssetTypeUid, string type)
        {
            var style = this.GetAssetTypeStyle(AssetTypeUid);
            if (style != null && !string.IsNullOrEmpty(style.Icon))
                return style.Icon;

            TopNavigationItem menuItem = null;

            if (AssetTypeUid != null)
            {
                //CTE query to get lower levels of children unioned with a query to get first level
                menuItem = Company.Query<TopNavigationItem>($@"WITH cteParents(Object, ObjectID, Subject, SubjectID, Level)
                    AS (SELECT it.Object, it.ObjectID, it.Subject, it.SubjectID, 1
                        FROM [dbo].[IntersectType] it 
                        INNER JOIN [dbo].[Predicate] p ON p.ID = it.PredicateID AND p.Type = 3
                        INNER JOIN [dbo].[AssetType] at ON at.Object = it.Object AND at.ObjectID = it.ObjectID
                        AND at.uid = '{AssetTypeUid.ToString()}'
                        UNION ALL
                        SELECT cteParents.Object, cteParents.ObjectID, it.Subject, it.SubjectID, cteParents.Level+1
                            FROM cteParents
                            INNER JOIN [dbo].[IntersectType] it ON it.Object = cteParents.Subject and it.ObjectID = cteParents.SubjectID
                            INNER JOIN [dbo].[Predicate] p ON P.ID = it.PredicateID and p.Type = 3)
                    SELECT nav.Icon, nav.ImageIconUrl
                    FROM cteParents
                    INNER JOIN SiteNav nav1 on cteParents.Subject = nav1.Object and cteParents.SubjectID = nav1.ObjectID
                    INNER JOIN SiteNav nav on nav1.ParentID = nav.ID
                    UNION ALL
                    SELECT nav.Icon, nav.ImageIconUrl
                    FROM [dbo].[AssetType] at
                    INNER JOIN SiteNav nav1 on at.Object = nav1.Object and at.ObjectID = nav1.ObjectID
                    INNER JOIN SiteNav nav on nav1.ParentID = nav.ID
                    WHERE at.uid = '{AssetTypeUid.ToString()}';").FirstOrDefault();
            }

            if (menuItem == null) {
                string siteNavName = null;

                switch (type)
                {
                    case "Resource":
                    case "User":
                        return "fa-user";
                    case "Group":
                        return "fa-users";
                    case "Grammatic Type":
                        return "fa-comments";
                    case "Attribute":
                        return "fa-pencil-square-o";
                    case "Fusion":
                    case "FusionType":
                        siteNavName = "#Fusion";
                        break;
                    case "Business Asset":
                        siteNavName = "#Business";
                        break;
                    case "Technical Asset":
                        siteNavName = "#Technical";
                        break;
                    case "Model":
                        siteNavName = "#Models";
                        break;
                    case "Reference":
                        siteNavName = "#Reference";
                        break;
                    case "Rule":
                        siteNavName = "#Data Quality";
                        break;
                    case "Policy":
                        siteNavName = "#Policy";
                        break;
                }

                if (siteNavName != null)
                {
                    menuItem = Company.Query<TopNavigationItem>($@" select Icon FROM [dbo].[SiteNav] WHERE Name = '{siteNavName}';").FirstOrDefault();
                }
            }

            if (menuItem != null)
            {
                if (menuItem.FullURL != null)
                    return menuItem.FullURL;
                else if (menuItem.Icon != null)
                    return menuItem.Icon;
            }

            return "fa-circle-o";
        }

        private async Task AugmentResult(TypeaheadResult result)
        {
            AddIcon(result);
            await AddAssetPath(result);
        }

        private async Task AugmentResult(IndexResult result)
        {
            await AugmentResult(result as TypeaheadResult);
            if (result.Uid.HasValue && result.Uid.Value != Guid.Empty)
            {
                result.Fields = await AssetRepository.GetAssetSearchFields(result.Uid ?? Guid.Empty);
            }
        }

        private void AddIcon(TypeaheadResult result)
        {
            string icon = GetIcon(result.AssetTypeUid, result.Group);
            if(icon.Substring(0, 3) == "fa-")
            {
                result.Icon = icon;
            } else
            {
                result.Icon = null;
                result.ImageUrl = icon;
            }
        }

        private async Task AddAssetPath(TypeaheadResult result)
        {
            if(result.Uid.HasValue && result.Uid.Value != Guid.Empty)
            {
                result.AssetPath = await AssetRepository.GetAssetPath(result.Uid ?? Guid.Empty);
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
                if (Community.GetCompanySettings().TryGetValue("HideData3SixtyUsers", out string val))
                {
                    limits.HideData3SixtyUsers = bool.Parse(val);
                }
            } else
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

    }
}
