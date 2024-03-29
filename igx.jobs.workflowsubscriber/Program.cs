using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
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
					c.AddAzureStorageQueues(q => {
						q.MaxPollingInterval = TimeSpan.FromSeconds(5);
#if DEBUG
						q.BatchSize = 1;
#endif
					}).AddServiceBus();
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
							StorageConnectionString = context.Configuration[constants.Setting.Storage]
						};
					});
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, MandrillMailProvider>(o => {
						return new MandrillMailProvider()
						{
							ApiKey = context.Configuration[constants.Setting.MailKey],
							SubAccount = context.Configuration[constants.Setting.MailAccount]
						};
					});
				});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
