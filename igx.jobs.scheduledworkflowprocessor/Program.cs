using d360.core;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace igx.jobs.scheduledworkflowprocessor
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddTimers();
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
					services.AddScoped<ICachingProvider, DummyCachingProvider>();
					services.AddScoped<IMailProvider, MandrillMailProvider>(s => {
						return new MandrillMailProvider { 
							ApiKey = context.Configuration["MandrillApiKey"],
							SubAccount = context.Configuration["MandrillSubAccount"]
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