using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddTimers()
					 .AddAzureStorageQueues(q => {
						 q.MaxPollingInterval = TimeSpan.FromSeconds(5);
					 });
				})
				.ConfigureGovernLogging()
				.AddScopedCommunity()
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, AzureQueueSource>(s => {
						return new AzureQueueSource
						{
							StorageConnectionString = context.Configuration[constants.Setting.Storage]
						};
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
