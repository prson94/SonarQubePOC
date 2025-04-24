using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using d360.extensions.storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace igx.jobs.apiexecutionprocessor
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
					services.AddScoped<IMailProvider, MandrillMailProvider>(s => {
						return new MandrillMailProvider
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
