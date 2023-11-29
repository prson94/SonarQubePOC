using System;
using System.Threading.Tasks;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using d360.extensions.storage;
using d360.featureflags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace igx.jobs.bulkloadprocessor
{
    public static class Program
    {
        public static async Task Main()
        {
			var builder = new HostBuilder();

			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddAzureStorageQueues();
					c.AddServiceBus(s => {
						s.MaxAutoLockRenewalDuration = new TimeSpan(0, 5, 0); // auto renew messages for 5 additional minutes.                    
						s.MaxConcurrentCalls = 5; // up to 5 concurrent calls.
					});
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, AzureQueueSource>(s => {
						return new AzureQueueSource
						{
							EventBusTopicName = context.Configuration["EventBusTopicName"],
							EventServiceBusConnectionString = context.Configuration["EventServiceBus"],
							QueuesConnectionString = context.Configuration["QueuesConnectionString"]
						};
					});
					services.AddScoped<IStorageProvider, AzureStorageProvider>(s => {
						return new AzureStorageProvider { StorageConnectionString = context.Configuration["MainStorageAccount"] };
					});
					services.AddSingleton<IFeatureFlagService, FeatureFlagService>(o => {
						return new FeatureFlagService(context.Configuration["LaunchDarklySdkKey"]);
					});
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, DummyMailProvider>();
				});

			System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
    }
}
