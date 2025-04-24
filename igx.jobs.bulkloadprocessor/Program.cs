using System;
using System.Threading.Tasks;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using d360.extensions.storage;
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
					 c.AddAzureStorageQueues(q => {
						  q.MaxPollingInterval = TimeSpan.FromSeconds(5);
#if DEBUG
						 q.BatchSize = 1;
#endif
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
					services.AddScoped<IStorageProvider, AzureStorageProvider>(s => {
						return new AzureStorageProvider { StorageConnectionString = context.Configuration[constants.Setting.Storage] };
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
