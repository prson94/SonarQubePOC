using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;

namespace igx.jobs.scoreprocessor
{
    public static class LegacyScoreProcessor
    {
        const string functionName = "Scoring_Calculate";
#if DEBUG
        const string timerSettings = "*/5 * * * * *";
#else
        const string timerSettings = "0 */5 * * * *";
#endif
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                //CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(x => x.CompanyID == 9).ToList();
#endif

                companies.AsParallel().WithDegreeOfParallelism(3).ForAll(c =>
                {
                    try
                    {
                        using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            company.Open();
                            company.Execute("metrics.LoadFromStaging", commandTimeout: 3600);
                            lock (log)
                            {
                                log.WriteLine("Processed scores for company {0}...", c.CompanyID);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        lock (log)
                        {
                            log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                        }
                    }
                });

                //CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }
        }
    }
}
