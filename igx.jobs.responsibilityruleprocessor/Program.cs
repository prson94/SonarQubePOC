using d360.core;
using d360.core.entities;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
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
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class ResponsibilityRuleProcessor
    {
        const string functionName = "ResponsibilityRules_ProcessScheduled";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else
        const string timerSettings = "0 */3 * * * *";
#endif
        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                foreach (var c in companies)
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

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
