using d360.core;
using d360.core.entities;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.responsibilityruleprocessor
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
            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class ResponsibilityRuleProcessor
    {
        const string functionName = "ResponsibilityRules_ProcessScheduled";
        const string timerSettings = "0 */3 * * * *";

        public static async Task Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 1).ToList();
#endif

                foreach (var c in companies)
                {
                    try
                    {
                        var company = JobDbContextCreator.CreateWebjobCompanyContext(c.CompanyID, 0, "", true);

                        CoreFunction.AITrackEvent(functionName, "ResponsibilityRuleProcessor Job Starting", new Dictionary<string, string> { { "CompanyID", c.CompanyID.ToString() } });

                        try
                        {
                            company.ClearInvalidRelationRuleResults();
                        }
                        catch (Exception dex)
                        {
                            CoreFunction.AITrackException(functionName, dex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{dex.GetFullExceptionData()}]");
                            CoreFunction.AIFlush();
                        }

                        try
                        {
                            await company.ProcessResponsibilityRelationRules();
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                            CoreFunction.AIFlush();
                        }

                        CoreFunction.AITrackEvent(functionName, "ResponsibilityRuleProcessor Job Completed", new Dictionary<string, string> { { "CompanyID", c.CompanyID.ToString() } });
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        CoreFunction.AIFlush();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
