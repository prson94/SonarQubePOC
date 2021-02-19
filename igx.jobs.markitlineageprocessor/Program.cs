using d360.core.enums;
using d360.core.enums.Workflow;
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

namespace igx.jobs.markitlineageprocessor
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

    public static class MarkitLineageProcessor
    {
        const string functionName = "Lineage_MarkitProcessor";
        

#if DEBUG
        const string timerSettings = "*/10 * * * * *"; // run in debug every 10 seconds
#else
        const string timerSettings = "0 */15 * * * *"; // every 15 minutes
#endif


        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                foreach (var c in companies)
                {
                    try
                    {
#if DEBUG
                        if (c.CompanyID != 1183)
                            continue;
#endif

                        // Create EF connection
                        var company = JobDbContextCreator.CreateWebjobCompanyContext(c.CompanyID, 0, c.UrlPrefix, true);

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
