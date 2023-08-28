using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace igx.functions.consumption.TelemetryInitializers
{
    internal class InjectCustomPropertiesInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
			if (!telemetry.Context.GlobalProperties.ContainsKey("Function"))
			{
				telemetry.Context.GlobalProperties.Add("Function", "PostExecutionJobProcessor");
			}

		}
    }
}
