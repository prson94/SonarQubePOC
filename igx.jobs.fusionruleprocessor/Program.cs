using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.fusionruleprocessor
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

    public static class FusionRuleProcessor
    {
        const string functionName = "Fusion_ProcessRules";
#if DEBUG
        const string timing = "0 * * * * *";
#else
        const string timing = "0 */10 * * * *";
#endif
        public static async Task Run([TimerTrigger(timing)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot().ToList();


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
