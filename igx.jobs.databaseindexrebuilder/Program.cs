using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace igx.jobs.databaseindexrebuilder
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
    public static class DatabasIndexRebuilder
    {
        const string functionName = "DatabaseTask_IndexRebuilder";
#if DEBUG
        const string timerSettings = "*/60 * * * * *";
#else
        const string timerSettings = "0 0 0 * * SAT";
#endif

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                List<string> failedReindexes = new List<string>();
                var companies = CoreFunction.GetCompaniesByCurrentSlot().OrderBy(x => x.Priority);
                foreach (var item in companies)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties.Add("Prefix", item.UrlPrefix);
                    try
                    {
                        CoreFunction.AITrackEvent(functionName, "Starting Index Rebuild", properties, item.CompanyID);
                        var start = DateTime.Now;
                        using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(item.CompanyID))
                        {
                            companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);
                            var res = companyConnection.Execute("EXEC [dbo].[AzureSQLMaintenance]", new { Operation = "reindex", From = 30, To = 100, MinNumberOfPages = 10 });                        
                        }
                        TimeSpan end = DateTime.Now - start;
                        properties.Add("Time Taken", end.TotalMilliseconds.ToString());
                        CoreFunction.AITrackEvent(functionName, "Completed Index Rebuild", properties, item.CompanyID);
                    }
                    catch (Exception e)
                    {
                        CoreFunction.AITrackException(functionName, e);
                        failedReindexes.Add(item.UrlPrefix);
                    }
                }
                if (failedReindexes.Any())
                {
                    CoreFunction.AITrackEvent(functionName, "Completed with errors.",
                        new Dictionary<string, string>()
                        {
                            { "Function", functionName },
                            { "Failed Items", string.Join(", ", failedReindexes.ToArray()) }
                        });
                }
                else
                {
                    CoreFunction.AITrackJobCompletedNoErrors(functionName);
                }
            }
            catch(Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

        }


    }

}
