using d360.core.enums;
using d360.core.enums.Workflow;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;

namespace igx.jobs.scheduledworkflowprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class ScheduledWorkflowProcessor
    {
        const string functionName = "Workflow_ProcessSchedule";
        const string timerSettings = "0 */15 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies = companies.Where(x => x.CompanyID == 4).ToList();

                companies.ForEach(c =>
                {
                    try
                    {
                        #region Create EF connection

                        var sec = new UriSecurityContextProvider()
                        {
                            CompanyID = c.CompanyID,
                            ResourceID = 0,
                            CompanyPrefix = c.UrlPrefix,
                            IsAdministrator = true
                        };
                        var cache = new DummyCachingProvider();
                        var queue = new AzureQueueSource();
                        var community = new CommunityContext(cache, queue, sec);
                        var company = new CompanyContext(community, cache, queue, sec, true);

                        #endregion

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
