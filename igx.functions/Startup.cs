using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using d360.extensions.search;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

[assembly: FunctionsStartup(typeof(igx.functions.consumption.Startup))]

namespace igx.functions.consumption
{
	internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
			var config = builder.GetContext().Configuration;
			builder.Services.AddSingleton(c => {
				return config;
			});
			builder.Services.AddLogging(l =>
			{
				var logLevel = LogLevel.Warning;
				var configLogLevel = config["LogLevel"];
				if (!string.IsNullOrEmpty(configLogLevel))
				{
					Enum.TryParse(configLogLevel, out logLevel);
				}
				l.SetMinimumLevel(logLevel);

				l.AddApplicationInsights(
					o => { o.ConnectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"]; }, 
					o => { o.FlushOnDispose = true; }
				);
			});
			builder.Services.AddScoped(o =>
			{
				return new ElasticSearchSource
				{
					CommunityConnectionString = config["CommunityContext"]
				};
			});
			builder.Services.AddScoped<IMailProvider, MandrillMailProvider>(s => {
				return new MandrillMailProvider
				{
					ApiKey = config["MandrillApiKey"],
					SubAccount = config["MandrillSubAccount"]
				};
			});
			builder.Services.AddScoped<IQueueSource, AzureQueueSource>(s => {
				return new AzureQueueSource
				{
					EventBusTopicName = config["EventBusTopicName"],
					EventServiceBusConnectionString = config["EventServiceBus"],
					QueuesConnectionString = config["QueuesConnectionString"]
				};
			});
			builder.Services.AddScoped<ICachingProvider, DummyCachingProvider>();
		}
	}
}
