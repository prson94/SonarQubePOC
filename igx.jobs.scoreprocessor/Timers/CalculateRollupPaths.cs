using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using d360.core.enums;

namespace igx.jobs.scoreprocessor
{
    public static class CalculateRollupPaths
    {
        const string functionName = "Scoring_CalculateRollupPaths_Timer";
        const string timerSettings = "0 */30 * * * *"; //every 30 minutes

        public static async Task Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#if DEBUG
                companies = companies.Where(x => x.CompanyID == 1).ToList();
#endif

                foreach(var c in companies)
                {
                    try
                    {
                        using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            company.Open();
                            await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);
                        }                          
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);                        
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);                
            }

            CoreFunction.AIFlush();
        }
    }
}
