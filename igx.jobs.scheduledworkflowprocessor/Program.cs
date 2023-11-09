using d360.core;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using DocumentFormat.OpenXml.EMMA;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scheduledworkflowprocessor
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage()
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public static class ScheduledWorkflowProcessor
    {
        const string FUNCTION_NAME = "Workflow_ProcessSchedule";

#if DEBUG
        const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 */15 * * * *";
#endif


		public static void Run([TimerTrigger(TIMER_SETTINGS)]TimerInfo myTimer, ILogger log)   
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

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
							// Create EF connection
							var company = JobDbContextCreator.CreateCompanyContext(
								new UriSecurityContextProvider
								{
									CompanyID = c.CompanyID,
									CompanyPrefix = c.UrlPrefix,
									ResourceID = 0,
									IsAdministrator = true
								},
								new MandrillMailProvider
								{
									ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
									SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
								},
								new AzureQueueSource(),
								new DummyCachingProvider(),
								constants.COMMUNITY_DATABASE_CONNECTION);

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
