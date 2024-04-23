using d360.core;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.mail;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
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
					c.AddExecutionContextBinding();
				})
				.ConfigureGovernLogging()
				.ConfigureServices((context, services) => {
					services.AddScoped<IQueueSource, AzureQueueSource>(s => {
						return new AzureQueueSource
						{
							StorageConnectionString = context.Configuration[constants.Setting.Storage]
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