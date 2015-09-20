using d360.extensions;
using d360.model;
using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [Authorize]
    public class AppsController : BaseController
    {
        #region DI

        ISearchSource SearchSource;

        public AppsController(CommunityContext community, CompanyContext company, ISearchSource searchSource)
            : base(community, company)
        {
            SearchSource = searchSource;
        }

        #endregion

        public ActionResult Search(string phrase)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewBag.Phrase = phrase;
            return View();
        }

        public ActionResult SearchBeta(string phrase)
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewBag.Phrase = phrase;
            return View();
        }
    }
}