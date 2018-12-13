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
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class WorkflowDigestProcessor
    {
        const string functionName = "Lineage_MarkitProcessor";
        

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 */15 * * * *"; // every 15 minutes
#endif


        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();


                foreach (var c in companies)
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

                        await company.GenerateMarkitBusinessLineage();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.Message}]");
                    }
                }
                

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
