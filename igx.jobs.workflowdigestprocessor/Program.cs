using d360.extensions;
using d360.extensions.caching;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.featureflags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace igx.jobs.workflowdigestprocessor
{
	class Program
	{
		static async Task Main()
		{
			var builder = new HostBuilder();

			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c =>
				{
					c.AddTimers();
					c.AddExecutionContextBinding();
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) =>
				{
					services.AddScoped<IQueueSource, DummyQueueSource>();
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, MandrillMailProvider>(o => {
						return new MandrillMailProvider()
						{
							ApiKey = context.Configuration[constants.Setting.MailKey],
							SubAccount = context.Configuration[constants.Setting.MailAccount]
						};
					});
					services.AddSingleton<IFeatureFlagService, FeatureFlagService>(o => {
						return new FeatureFlagService(context.Configuration[constants.Setting.FeatureFlagKey]);
					});
				});

			//add random delay so instances run at an offset to each other.
			var rand = new Random();
			Thread.Sleep(rand.Next(301) * 1000);

			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
	}
}