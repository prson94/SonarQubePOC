using Microsoft.ApplicationInsights;
using System.Web.Http.ExceptionHandling;

namespace d360.web.Models.Attributes
{
    public class AiExceptionLogger : ExceptionLogger
    {
        public TelemetryClient Telemetry { get; set; }

        public AiExceptionLogger()
        {
            Telemetry = new TelemetryClient();
        }

        public override void Log(ExceptionLoggerContext context)
        {
            if (context != null && context.Exception != null)
            {
                var Telemetry = new TelemetryClient();
                Telemetry.TrackException(context.Exception);
            }
            base.Log(context);
        }
    }
    //public class AiExceptionFilterAttribute : ExceptionFilterAttribute
    //{
    //    public TelemetryClient Telemetry { get; set; }

    //    public AiExceptionFilterAttribute()
    //    {
    //        Telemetry = new TelemetryClient();
    //    }

    //    public override void OnException(HttpActionExecutedContext actionExecutedContext)
    //    {
    //        if (actionExecutedContext != null && actionExecutedContext.Exception != null)
    //        {
    //            //var Telemetry = new TelemetryClient();
    //            Telemetry.TrackException(actionExecutedContext.Exception);
    //        }
    //        base.OnException(actionExecutedContext);
    //    }
    //}
}
