using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;
using d360.model;
using d360.extensions;
using d360.core;
using d360.web.Models;

namespace d360.web.Controllers
{
    [Authorize]
    public class SearchController : BaseController
    {
        #region DI

        ISearchSource SearchSource;

        public SearchController(
            CommunityContext community,
            CompanyContext company, 
            ISearchSource searchSource)
            : base(community, company)
        {
            SearchSource = searchSource;
        }

        #endregion

        #region Json

        [HttpPost]
        public JsonResult Results(string search, int? size, int? from, string group, string type, string adv)
        {
            var o = new SearchResultsViewModel();

            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(adv))
            {
                o.Result = SearchSource.GetSearchResultsWithCategory(Company.CurrentCompanyID, Company.CurrentResourceID, search, size.GetValueOrDefault(100), from.GetValueOrDefault(0), o.Categories, group, type, adv);
            }

            return Json(o);
        }

        [HttpGet]
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



        [HttpGet]
        public JsonResult Typeahead(string q, string t)
        {
            if (!string.IsNullOrEmpty(q))
            {
                IEnumerable<TypeaheadResult> res = SearchSource.GetTypeaheadResults(Company.CurrentCompanyID, Company.CurrentResourceID, q, 20, t);

                return Json(res, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}
