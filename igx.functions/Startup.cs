using igx.functions.consumption.TelemetryInitializers;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(igx.functions.consumption.Startup))]

namespace igx.functions.consumption
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<ITelemetryInitializer, ExceptionToResponseCodeInitializer>();
        }
    }
}
