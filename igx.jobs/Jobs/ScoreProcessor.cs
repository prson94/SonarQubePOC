using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;

namespace igx.jobs
{
    public static class ScoreProcessor
    {
        const string functionName = "Scoring_Calculate";
        const string timerSettings = "0 */10 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        company.Execute("metrics.LoadFromStaging", commandTimeout: 1400); //utility.CalculateScores
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.WriteLine($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.WriteLine($"General Exception: {ex.GetFullExceptionData()}");
            }
        }
    }
}
