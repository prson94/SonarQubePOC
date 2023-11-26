using d360.core;
using d360.core.enums;
using d360.core.enums.Workflow;
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
using System.Data.Entity;
using System.Linq;
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
							ApiKey = context.Configuration[constants.MAIL_API_KEY],
							SubAccount = context.Configuration[constants.MAIL_SUB_ACCOUNT]
						};
					});
				});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class ScheduledWorkflowProcessor : BaseWebJob
	{
        const string FUNCTION_NAME = "Workflow_ProcessSchedule";

#if DEBUG
        const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 */15 * * * *";
#endif

		ICachingProvider Cache;
		IMailProvider Mail;
		IQueueSource Queue;

		public ScheduledWorkflowProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		public void Run([TimerTrigger(TIMER_SETTINGS)]TimerInfo myTimer, ILogger log)   
        {
			try
			{
				var companies = GetCompaniesByCurrentSlot();

				companies.ForEach(c =>
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
								CompanyPrefix = c.UrlPrefix,
								ResourceID = 0,
								IsAdministrator = true
							};
							var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
							var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);

							// Load all workflows of type schedule.
							var scheduledWorkflows = company.WorkflowEventRegistrations.Where(x => x.ChangeType == ChangeType.Schedule && x.Type.State == State.Active && x.Type.PublishedVersionID != null).Include(x => x.Type).ToList();

							foreach (var registration in scheduledWorkflows)
							{
								// If the registration applies fire of the workflow and break if not go to the next one.
								if (company.ExecuteScheduledWorkflow(registration).Result)
								{
									break;
								}
							}

							var res = company.ExecuteTimerSteps();
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error processing scheduled workflows for this environment.");
						}
					}
				});
			}
			catch (Exception ex)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME }
				};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error when running scheduled workflows.");
				}
			}
        }
    }
}