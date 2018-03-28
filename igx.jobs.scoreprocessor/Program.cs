using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;

namespace igx.jobs.scoreprocessor
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

    public static class ScoreProcessor
    {
        const string functionName = "Scoring_Calculate";
        const string timerSettings = "0 */5 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
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
                        //company.Execute("metrics.LoadFromStaging", commandTimeout: 1400);

                        bool processStatus = false;
                        var processTask = company.ExecuteAsync("metrics.LoadFromStaging", commandTimeout: 1400);
                        processTask.ContinueWith(t =>
                        {
                            string exceptionData = "";
                            if (t.Exception != null)
                            {
                                exceptionData = t.Exception.GetFullExceptionData();
                                if (t.Exception.InnerExceptions != null)
                                {
                                    foreach (var ex in t.Exception.InnerExceptions)
                                    {
                                        exceptionData += ex.GetFullExceptionData();
                                    }
                                }
                                CoreFunction.AITrackException(functionName, t.Exception, c.CompanyID);
                            }

                            if (t.IsCompleted)
                            {
                                if (t.IsFaulted)
                                {
                                    CoreFunction.AITrackException(functionName, t.Exception, c.CompanyID);
                                }
                            }

                            processStatus = false;
                        });

                        while (processStatus && (processTask.Exception == null))
                        {
                            log.WriteLine("Processing scores for company {0}...", c.CompanyID);
                            System.Threading.Thread.Sleep(30000);
                        }
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
