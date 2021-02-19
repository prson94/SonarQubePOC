using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
    class Program
    {
        static async Task Main()
        {
            using (var host = CoreFunction.JobHostConfig())
            {
                await host.RunAsync();
            }
        }
    }

    public static class WorkflowDigestProcessor
    {
        const string functionName = "Workflow_DigestProcessor";
        

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 0 5 * * *"; // every day at 5am
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
                        // Create EF connection
                        var company = JobDbContextCreator.CreateWebjobCompanyContext(c.CompanyID, 0, c.UrlPrefix, true);

                        await company.SendDigestEmails(c.EnvironmentLevel);
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