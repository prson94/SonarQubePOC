using d360.extensions;
using d360.model;
using d360.web.Models;
using d360.web.Models.Attributes;
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
                    IEnumerable<TypeaheadResult> res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, Company.CurrentResourceID, q, num.GetValueOrDefault(7), t);

                    return new JsonNetResult { Data = res, Formatting = Newtonsoft.Json.Formatting.None };
                }

                return new JsonNetResult { Data = null };
            }
            catch (System.Exception ex)
            {
                return jsonNetException(ex);
            }
        }
        
        #endregion
    }
}
