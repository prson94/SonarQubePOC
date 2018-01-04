using igx.jobs.fusion.rules;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;

namespace igx.jobs
{
    public static class FusionRuleProcessor
    {
        const string functionName = "Fusion_ProcessRules";
        //const string timing = "0 * * * * *";
        const string timing = "0 */30 * * * *";

        public static void Run([TimerTrigger(timing)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    //CoreFunction.AITrackTrace(functionName, $"Starting Fusion Rule Process for company id[{c.CompanyID}]", companyId: c.CompanyID);

                    try
                    {
                        Processor.Process(c.CompanyID, log);
                    }
                    catch (Exception ex)
                    {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }

                    //CoreFunction.AITrackTrace(functionName, $"Completed Fusion Rule Process for company id[{c.CompanyID}]", companyId: c.CompanyID);
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }
    }
}
