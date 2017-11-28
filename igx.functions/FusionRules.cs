using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using d360.core;
using igx.functions.fusion.Rules;

namespace igx.functions
{
    public static class FusionRulesFunction
    {
        const string functionName = "ProcessFusionRules";
        //const string timing = "0 * * * * *";
        const string timing = "0 */30 * * * *";

        [FunctionName(functionName), Disable("FusionRules_Disabled")]
        public static void Run([TimerTrigger(timing)]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Fusion Rule trigger function executed at: {DateTime.Now}");

            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {                    
                    log.Info($"Starting Fusion Rule Process for company id[{c.CompanyID}]");

                    try
                    {
                            FusionRuleProcessor.Process(c.CompanyID, log);
                    }
                    catch (Exception ex)
                    {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }

                    log.Info($"Completed Fusion Rule Process for company id[{c.CompanyID}]");                        
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
