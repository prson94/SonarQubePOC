using d360.core;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.featureflags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

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
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) =>
				{
					services.AddScoped<IQueueSource, DummyQueueSource>();
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, MandrillMailProvider>(o => {
						return new MandrillMailProvider()
						{
							ApiKey = context.Configuration[constants.MAIL_API_KEY],
							SubAccount = context.Configuration[constants.MAIL_SUB_ACCOUNT]
						};
					});
					services.AddSingleton<IFeatureFlagService, FeatureFlagService>(o => {
						return new FeatureFlagService(context.Configuration["LaunchDarklySdkKey"]);
					});
				});
			
			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
	}
}