using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [Authorize, RoutePrefix("search")]
    public class SearchController : BaseController
    {
        #region DI

        ISearchSource SearchSource;

        public SearchController(
            ICommunityContext community,
            ICompanyContext company, 
            ISearchSource searchSource)
            : base(community, company)
        {
            SearchSource = searchSource;
        }

        #endregion

        #region Json

        [HttpPost, Route("Results"), NonNullableParameters]
        public JsonResult Results(QueryRequest queryRequest)
        {
            var o = new SearchResultsViewModel();

            if (!string.IsNullOrEmpty(queryRequest.Term))
            {
                o.Result = SearchSource.GetSearchResultsWithAggregation(Company.CurrentCompanyID, Company.CurrentResourceID, queryRequest, o.Categories);

                foreach (IndexResult result in o.Result.Results)
                {
                    AddIcon(result);
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
        public JsonNetResult Typeahead(string q, string t, int? num)
        {
            try
            {
                if (!string.IsNullOrEmpty(q))
                {
                    IList<TypeaheadResult> res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, Company.CurrentResourceID, q, num.GetValueOrDefault(7), t).ToList();
                    foreach(TypeaheadResult result in res)
                    {
                        AddIcon(result);
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

        private string GetIcon(Guid? AssetTypeUid, string type)
        {
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

        private TypeaheadResult AddIcon(TypeaheadResult result)
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
            return result;
        }

        #endregion
    }
}
