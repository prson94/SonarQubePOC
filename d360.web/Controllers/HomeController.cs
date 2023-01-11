using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model.DataAccessLayer;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [HandleError(View = "Error")]
    public class HomeController : BaseController
    {
        #region DI

        private readonly ICachingProvider Cache;

        public HomeController(ICoreComponentSet set, ICachingProvider cache)
            : base(set)
        {
            Cache = cache;
        }

        #endregion

        [AllowAnonymous, Route("unsupported")]
        public async Task<ActionResult> Unsupported()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            await this.AppendSettingsToViewData();
            return View("Unsupported");
        }

        /// <summary>
        /// Angular SPA
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<ActionResult> App()
        {
            ViewData.Add("VersionNumber", typeof(HomeController).Assembly.GetName().Version);
            ViewData.Add("ResourceID", Company.CurrentResourceID);
            ViewData.Add("ResourceHomePage", Company.GetUserHomePage());

            await this.AppendSettingsToViewData(httpContext: System.Web.HttpContext.Current);

            ViewData.Add("EnvironmentSettings", new Dictionary<string, string> { { "HelpBaseUri", System.Configuration.ConfigurationManager.AppSettings["FluidTopicBaseUri"].ToString() } });
            ViewData.Add("SingleSignOn", await IsSingleSignOn());

            var res = Company.GlobalReportingResources.Where(x => x.ResourceID == Company.CurrentResourceID).FirstOrDefault();

            if (res != null)
            {
                ViewData.Add("ResourceName", res.FullName);
                ViewData.Add("ResourceEmail", res.Email);
                ViewData.Add("ResourceUid", res.Uid);
            }
            else
            {
                ViewData.Add("ResourceName", "");
                ViewData.Add("ResourceEmail", "");
                ViewData.Add("ResourceUid", "");
            }
            return View("App");
        }

        /// <summary>
        /// Fallback for incorrect API URLs
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;

            return Json(
                new
                {
                    title = "Error",
                    message = "The requested URL was not found. Please check the URL and all parameters are correct."
                },
                JsonRequestBehavior.AllowGet);
        }
    }
}
