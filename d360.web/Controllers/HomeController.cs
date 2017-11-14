using System.Web.Mvc;
using d360.core.entities;
using d360.model;
using System.Linq;

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

        /// <summary>
        /// Angular SPA
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult App()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("ResourceHomePage", Company.GetUserHomePage());
            ViewData.Add("Settings", Community.GetCompanySettings());
            ViewData.Add("SingleSignOn", IsSingleSignOn());

            var res = Company.GlobalReportingResources.Where(x => x.ResourceID == Company.CurrentResourceID).FirstOrDefault();
            if (res != null) {
                ViewData.Add("ResourceName", res.FullName);
                ViewData.Add("ResourceEmail", res.Email);
            }
            return View("App");
        }
    }
}
