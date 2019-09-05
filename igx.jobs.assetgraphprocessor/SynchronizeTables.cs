using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions;
using igx.jobs;
using d360.utils.company;
using System.Data;
using System.Data.SqlClient;
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
        const string timerSettings = "0 0 3 * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
#if DEBUG
            var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 4).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif


            foreach (var company in companies)
            {
                try
                {
                    var conn = CompanyConnectionUtils.GetCompanyConnection(company.CompanyID, company.Server, company.Username, company.Password);

                    using (conn)
                    {
                        const int timeout = 1000 * 60 * 10;

                        conn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        try
                        {
                            await conn.ExecuteAsync("graph.SynchronizeTables", commandTimeout: timeout);
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
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }

}
