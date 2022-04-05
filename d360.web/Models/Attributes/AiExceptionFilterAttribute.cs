using System.Web.Http.ExceptionHandling;

using Microsoft.ApplicationInsights;

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
}
