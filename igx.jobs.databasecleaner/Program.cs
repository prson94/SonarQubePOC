using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Hosting;

namespace igx.jobs.databasecleaner
{
    class Program
    {
        static async Task Main()
        {
            using(var host = CoreFunction.JobHostConfig())
            {
                await host.RunAsync();
            }
        }
    }

    public static class DatabaseCleaner
    {
        const string functionName = "DatabaseMaintenance_Cleaner";

#if DEBUG
        const string timerSettings = "*/1 * * * * *";
#else
        const string timerSettings = "0 0 4 * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

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

                            //remove any old api execution records
                            await company.ExecuteAsync("[api].[DeleteExecutionRecords]", commandTimeout: 1800);

                            //update database statistics
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
