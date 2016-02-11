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
        public JsonResult Results(string search)
        {
            var o = new SearchResultsViewModel();
            
            if (!string.IsNullOrEmpty(search))
            {
                o.Result = SearchSource.GetSearchResults(Company.CurrentCompanyID, Company.CurrentResourceID, search);
                o.Categories = o.Result.Results.GroupBy(i => i.Type).Select(i => new IndexCategory { ResultCount = i.Count(), Name = i.Key }).ToList();
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

        #endregion
    }
}
