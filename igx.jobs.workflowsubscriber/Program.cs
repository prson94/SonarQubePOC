using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using d360.featureflags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddServiceBus();
				})
				.ConfigureGovernLogging()
				.ConfigureWebJobs(c => {
					c.AddServiceBus(s =>
					{
						s.MaxAutoLockRenewalDuration = new TimeSpan(0, 5, 0); // auto renew messages for 5 additional minutes.                    
						s.MaxConcurrentCalls = 25; // up to 25 concurrent calls.
					});
				})
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, AzureQueueSource>(s => {
						return new AzureQueueSource
						{
							EventBusTopicName = context.Configuration["EventBusTopicName"],
							EventServiceBusConnectionString = context.Configuration["EventServiceBus"],
							QueuesConnectionString = context.Configuration["QueuesConnectionString"]
						};
					});
					services.AddSingleton<IFeatureFlagService, FeatureFlagService>(o => {
						return new FeatureFlagService(context.Configuration["LaunchDarklySdkKey"]);
					});
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, DummyMailProvider>();
				});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
