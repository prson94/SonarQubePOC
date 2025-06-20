using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

IConfigurationRoot configuration = new ConfigurationBuilder().AddConfiguration(builder.Configuration)
			  //.AddJsonFile("AppSettings.json")
			  .Build();

builder.Services.AddOpenTelemetry()
	//.WithTracing(o => { 
	//})
	//.WithLogging(o => {
	//	o.AddAzureMonitorLogExporter();
	//})
	.UseAzureMonitorExporter(o => {
		o.EnableLiveMetrics = true;
	});
builder.Services.ConfigureOpenTelemetryMeterProvider((sp, builder) => builder.AddMeter("OTel.Govern.QueueMessageCount"));

builder.Build().Run();
