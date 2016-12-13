using OptimaJet.Workflow;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Bus;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.Workflow.Core.Parser;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;
using WorkflowRuntime = OptimaJet.Workflow.Core.Runtime.WorkflowRuntime;
using OptimaJet.Workflow.DbPersistence;
using d360.model;
using d360.web.Controllers;

namespace d360.web.Controllers
{
    [RoutePrefix("designer"), Authorize]
    public class DesignerController : BaseController
    {
        #region DI

        public DesignerController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [HttpGet, Route("")]
        public ActionResult Index(string schemeName)
        {
            ViewBag.SchemeName = schemeName ?? "SimpleWF";
            return View();
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post), Route("api")]
        public ActionResult API()
        {
            Stream filestream = null;
            if (Request.Files.Count > 0)
                filestream = Request.Files[0].InputStream;

            var pars = new NameValueCollection();
            pars.Add(Request.Params);
            
            if(Request.HttpMethod.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
            {
                var parsKeys = pars.AllKeys;
                foreach (var key in Request.Form.AllKeys)
                {
                    if (!parsKeys.Contains(key))
                    {
                        pars.Add(Request.Form);
                    }
                }
            }

            var res = getRuntime.DesignerAPI(pars, filestream, true);
            if (pars["operation"].ToLower() == "downloadscheme")
                return File(Encoding.UTF8.GetBytes(res), "text/xml", "scheme.xml");
            return Content(res);
        }

      private WorkflowRuntime getRuntime
        {
            get
            {
                var builder = new WorkflowBuilder<XElement>(
                    new MSSQLProvider(Company.CompanyConnectionString),
                    new XmlWorkflowParser(),
                    new MSSQLProvider(Company.CompanyConnectionString)
                    ).WithDefaultCache();

                var runtime = new WorkflowRuntime(new Guid("{8D38DB8F-F3D5-4F26-A989-4FDD40F32D9D}"))
                    .WithBuilder(builder)
                    .DisableCodeActions()
                    //.WithTimerManager(new TimerManager())
                    .WithPersistenceProvider(new MSSQLProvider(Company.CompanyConnectionString))
                    .WithTimerManager(new TimerManager())
                    .WithBus(new NullBus())
                    .SwitchAutoUpdateSchemeBeforeGetAvailableCommandsOn()
                    .Start();

                return runtime;
            }
        }
    }
}


 