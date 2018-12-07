using d360.utils.company;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace igx.jobs.databasecleaner
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            config.UseTimers();
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class DatabaseCleaner
    {
        const string functionName = "DatabaseMaintenance_Cleaner";
        const string timerSettings = "0 0 4 * * *";
       // const string timerSettings = "*/10 * * * * *";

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(x => x.CompanyID == 4).ToList();
#endif

                foreach(var c in companies)
                {
                    try
                    {
                        using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            company.OpenWithRetry(RetryPolicy.DefaultProgressive);                        
                            await company.ExecuteAsync("sp_updatestats", commandTimeout: 1400);
                        }                          
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);                        
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
