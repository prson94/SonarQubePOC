using System.Web.Mvc;
using d360.core.entities;
using d360.model;

namespace d360.web.Controllers
{
    [HandleError(View = "Error")]
    public class HomeController : BaseController
    {
        #region DI

        public HomeController(CommunityContext community, CompanyContext company)
            : base(community, company) 
        { }

        #endregion

        [Authorize]
        public ActionResult Index()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View("SPA");
        }

        public ActionResult Main()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View();
        }

        public ActionResult Ember()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("Settings", Community.GetCompanySettings());
            return View("Core");
        }
    }
}
