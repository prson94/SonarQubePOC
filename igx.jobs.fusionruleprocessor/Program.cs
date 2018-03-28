using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
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

    public static class FusionRuleProcessor
    {
        const string functionName = "Fusion_ProcessRules";
        // const string timing = "0 * * * * *";
        const string timing = "0 */30 * * * *";

        public static async Task Run([TimerTrigger(timing)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                foreach (var company in companies)
                {
                    try
                    {
                        await Processor.Process(company.CompanyID, log);
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, company.CompanyID);
                    }
                }

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
