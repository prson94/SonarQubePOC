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
        public JsonResult Results(string search, int? size, int? from, string group, string type, string adv)
        {
            var o = new SearchResultsViewModel();

            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(adv))
            {
                o.Result = SearchSource.GetSearchResultsWithCategory(Company.CurrentCompanyID, Company.CurrentResourceID, search, size.GetValueOrDefault(100), from.GetValueOrDefault(0), o.Categories, group, type, adv);
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
            var sw = new Stopwatch();
            sw.Start();

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

        private string GetIcon(Guid? Uid, string type)
        {
            string icon = null;
            if (Uid != null)
            {
                icon = Company.Query<string>($@" select Icon FROM [dbo].[SiteNav] WHERE ID = (
                    SELECT TOP 1 s.ParentId
                    FROM [dbo].[Asset] a
                    INNER JOIN [dbo].[AssetType] at on a.AssetTypeID = at.ID
                    INNER JOIN [dbo].[SiteNav] s on at.Object = s.Object and at.ObjectID = s.ObjectID
                    WHERE a.uid = '{Uid.ToString()}');").FirstOrDefault();
                if (icon != null)
                    return icon;
            }

            string siteNavName = null;

            switch (type)
            {
                case "Resource":
                    return "fa-user";
                case "Group":
                    return "fa-users";
                case "Fusion":
                case "FusionAttributes":
                case "FusionType":
                    siteNavName = "#Fusion";
                    break;
                case "Artifact":
                case "Glossary":
                case "Grammatic Type":
                    siteNavName = "#Glossary";
                    break;
                case "Model":
                case "Taxonomy":
                    siteNavName = "#Models";
                    break;
                case "Reference":
                    siteNavName = "#Reference";
                    break;
                case "Rule":
                    siteNavName = "#Data Quality";
                    break;
            }
            //For typeahead results, Type is a concatenation of Type and subtype for Artifacts
            if (siteNavName == null && type.Length >= 8 && type.Substring(0, 8) == "Glossary")
                siteNavName = "#Glossary";

            if (siteNavName != null)
            {
                icon = Company.Query<string>($@" select Icon FROM [dbo].[SiteNav] WHERE Name = '{siteNavName}';").FirstOrDefault();
                if (icon != null)
                    return icon;
            }

            return "fa-circle-o";
        }

        private TypeaheadResult AddIcon(TypeaheadResult result)
        {
            result.Icon = GetIcon(result.Uid, result.Type);
            return result;
        }
        private IndexResult AddIcon(IndexResult result)
        {
            result.Icon = GetIcon(result.Uid, result.Group);
            return result;
        }

        #endregion
    }
}
