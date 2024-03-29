using d360.extensions;
using d360.extensions.caching;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.extensions.search;
using d360.extensions.storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace igx.jobs.indexer
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
					 .AddAzureStorageQueues();
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, DummyQueueSource>();
					services.AddScoped<IStorageProvider, DummyStorageProvider>();
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, DummyMailProvider>();
					services.AddScoped(s => {
						return new ElasticSearchSource
						{
							CommunityConnectionString = context.Configuration[constants.Setting.Community]
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
