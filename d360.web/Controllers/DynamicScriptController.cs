using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Configuration;
using System.ComponentModel.Composition;

namespace d360.web.Controllers
{
    public class DynamicScriptController: Controller
    {
        public JavaScriptResult Index()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("var apiUrl = '{0}';", ConfigurationManager.AppSettings["D360BaseUrl"]);
            builder.AppendFormat("var currentResourceID = '{0}';", 1); //ctx.CurrentResourceID
            return JavaScript(builder.ToString());
        }
    }
}
