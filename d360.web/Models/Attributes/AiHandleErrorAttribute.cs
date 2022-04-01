using System;
using System.Web.Mvc;

using d360.core;
using d360.web.Controllers;

using Microsoft.ApplicationInsights;

namespace d360.web.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AiHandleErrorAttribute : HandleErrorAttribute
    {
        public TelemetryClient Telemetry { get; set; }

        public AiHandleErrorAttribute()
        {
            Telemetry = new TelemetryClient();
        }

        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext != null && filterContext.HttpContext != null && filterContext.Exception != null)
            {
                //If customError is Off, then AI HTTPModule will report the exception
                if (filterContext.HttpContext.IsCustomErrorEnabled)
                {
                    Telemetry.TrackException(filterContext.Exception);
                }
            }

            filterContext.Result = new JsonNetResult
            {
                Data = new { type = "error", title = "Error Occurred!", message = filterContext.Exception.GetFullExceptionData() },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }
    }
}
