using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace igx.functions.consumption.TelemetryInitializers
{
    internal class ExceptionToResponseCodeInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is ExceptionTelemetry)
            {
                //This context is a kind of Singleton for the whole Request lifecycle
                //It means that we may use it to share some data between ITelemetry instances 
                telemetry.Context.GlobalProperties.Add("HasError", "true");
            }

            if (telemetry is RequestTelemetry)
            {
                if (telemetry.Context.GlobalProperties["HasError"].Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    ((RequestTelemetry)telemetry).ResponseCode = "500";
                }
            }
        }
    }
}
