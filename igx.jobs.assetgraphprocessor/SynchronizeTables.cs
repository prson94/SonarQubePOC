using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.utils.company;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace igx.jobs.assetgraphprocessor
{
    public class SynchronizeTables
    {
        const string functionName = "AssetGraphProcessor_SynchronizeTables";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else
        const string timerSettings = "0 0 1 * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
#if DEBUG
            var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 1).ToList();
#else
            var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

            var populatePaths = DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday;

            companies.AsParallel().WithDegreeOfParallelism(5).ForAll(async company => {
                try
                {
                    var conn = CompanyConnectionUtils.GetCompanyConnection(company.CompanyID, company.Server, company.Username, company.Password);

                    using (conn)
                    {
                        const int timeout = 60 * 180; //3 hours

                        try
                        {
                            conn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                            await conn.ExecuteAsync("graph.SynchronizeTables @populatePaths", new { populatePaths }, commandTimeout: timeout);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, company.CompanyID);
                        }

                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, company.CompanyID);
                }
            });


#if DEBUG  
            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
#endif

        }
    }
}
