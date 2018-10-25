using Microsoft.Azure.WebJobs;
using System;
using System.IO;

namespace igx.jobs.dataqualitychecker
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

    public static class DataQualityChecker
    {
        const string functionName = "DatabaseMaintenance_QualityChecker";
        const string timerSettings = "0 */30 * * * *";
        //const string timerSettings = "*/10 * * * * *";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log) //   
        {
            try
            {                
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
             
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);                        
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);                
            }

            CoreFunction.AIFlush();
        }
    }
}
