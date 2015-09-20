using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    [Authorize]
    public class OfficeAppsController : BaseController
    {
        #region DI

        public OfficeAppsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        public ActionResult Excel()
        {
            return View();
        }
    }
}