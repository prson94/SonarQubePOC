using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.core.entities;
using System.Web.Helpers;
using d360.web.Models;
using d360.extensions;
using System.Web.Security;
using d360.core.entities.Views;
using System.Net;

namespace d360.web.Controllers
{
    [HandleError(View = "Error"), Authorize]
    public class RedirectController : Controller
    {
        [Authorize]
        public RedirectResult File(string path)
        {
            return new RedirectResult(@"file://" + Server.UrlDecode(path).Replace(@"file://", "").Replace("\\", "/"));
        }
    }
}
