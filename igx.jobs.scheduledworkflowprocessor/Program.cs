using d360.core;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
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
        const string functionName = "Workflow_ProcessSchedule";
        

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 */15 * * * *";
#endif


        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = d360.utils.company.CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings();
                companies = companies.Where(x => x.CompanyID == 4).ToList();
#endif

                companies.ForEach(c =>
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
                            new DummyCachingProvider());

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
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.Message}]");
                    }
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.Message}");
            }

            CoreFunction.AIFlush();
        }
    }
}
