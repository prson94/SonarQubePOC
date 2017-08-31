using d360.core;
using d360.utils.company;
using Dapper;
using igx.functions.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Configuration;
using System.Linq;

namespace igx.functions.FusionRulesEngine
{
    public static class Engine
    {
    const string functionName = "ExecuteFusionRulesEngine";
    const string timerSettings = "0 */15 * * * *";
    //const string timerSettings = "*/10 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        //company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        bool writeStatus = true;

                        var task = company.ExecuteAsync("EXEC fusion.Rules", null, null, 12600);

                        task.ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                                log.Info($"Fusion promotion completed for Company {c.CompanyID}");
                            if (t.IsFaulted)
                                log.Error($"Fusion promotion failed for Company {c.CompanyID}");
                            writeStatus = false;
                        });

                        while (writeStatus)
                        {
                            System.Threading.Thread.Sleep(15000);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }
        }
    }
}
