using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.admin.ui.Controllers
{
    public class PagesController : Controller
    {
        public ActionResult One()
        {
            return View();
        }
        public ActionResult Two(int id = 1)
        {
            ViewBag.id = id;
            return View();
        }

        [Authorize]
        public ActionResult Three()
        {
            return View();
        }
    }
}