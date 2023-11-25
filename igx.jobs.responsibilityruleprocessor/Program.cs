using d360.extensions;
using d360.extensions.caching;
using d360.extensions.events;
using d360.extensions.info;
using d360.extensions.mail;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.responsibilityruleprocessor
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
					services.AddScoped<IMailProvider, DummyMailProvider>();
				});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class ResponsibilityRuleProcessor : BaseWebJob
	{
        const string FUNCTION_NAME = "ResponsibilityRules_ProcessScheduled";
        const string TIMER_SETTINGS = "0 */3 * * * *";

		ICachingProvider Cache;
		IMailProvider Mail;
		IQueueSource Queue;

		public ResponsibilityRuleProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)]TimerInfo myTimer, ILogger log)  
        {
			try
			{
				// increase the default dapper timeout from 30 to 90 seconds
				Dapper.SqlMapper.Settings.CommandTimeout = 90;

				var companies = GetCompaniesByCurrentSlot();

				foreach (var c in companies)
				{
					var logProperties = new Dictionary<string, object> {
						{ "Function", FUNCTION_NAME },
						{ "CompanyID", c.CompanyID },
						{ "UrlPrefix", c.UrlPrefix }
					};

					using (log.BeginScope(logProperties))
					{
						try
						{
							var context = new UriSecurityContextProvider
							{
								CompanyID = c.CompanyID,
								CompanyPrefix = "",
								ResourceID = 0,
								IsAdministrator = true
							};
							var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
							var company = new CompanyContext(community, Cache, Queue, Mail, context, true);

							try
							{
								company.ClearInvalidRelationRuleResults();
							}
							catch (Exception dex)
							{
								log.LogError(dex, "Error while clearing relation rules results.");
							}

							try
							{
								await company.ProcessResponsibilityRelationRules();
							}
							catch (Exception ex)
							{
								log.LogError(ex, "Error while processing responsibility rules.");
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error occurred while processing tasks for this environment.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.LogCritical(ex, "Critical exception at root of web job");
			}
        }
    }
}
