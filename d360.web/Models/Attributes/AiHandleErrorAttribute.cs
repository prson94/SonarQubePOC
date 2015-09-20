using Microsoft.ApplicationInsights;
using System;
using System.Web.Mvc;

namespace d360.web.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)] 
    public class AiHandleErrorAttribute: HandleErrorAttribute
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
                    //var Telemetry = new TelemetryClient();
                    Telemetry.TrackException(filterContext.Exception);
                }
            }
            base.OnException(filterContext);
        }
    }
}
