using System.Web;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Owin;

namespace d360.web
{
    /*
     * Custom TelemetryInitializer that sets the user ID, session ID and browser version for requests that get logged to app insights..
     *
     */
    public class GovernAppInsightsTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            var ctx = HttpContext.Current;

            // If telemetry initializer is called as part of request execution and not from some async thread
            if (ctx != null && ctx.Request != null && (string.IsNullOrEmpty(telemetry.Context.User.Id) || string.IsNullOrEmpty(telemetry.Context.Session.Id)))
            {
                IOwinContext oCtx;
                int resourceId = 0;

                try
                {
                    oCtx = ctx.Request.GetOwinContext();

                    if (oCtx != null)
                    {
                        resourceId = oCtx.Get<int>("ResourceID");
                    }

                    // Set the user id on the Application Insights telemetry item.
                    telemetry.Context.User.Id = resourceId.ToString();

                    // Set the session id on the Application Insights telemetry item.
                    telemetry.Context.Session.Id = resourceId.ToString();
                }
                catch
                {

                }

                if (!string.IsNullOrEmpty(ctx.Request.UserAgent) && string.IsNullOrEmpty(telemetry.Context.User.UserAgent))
                {
                    telemetry.Context.User.UserAgent = ctx.Request.UserAgent;
                }
            }
        }
    }
}
