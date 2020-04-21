using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.utils.company;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using d360.core.enums;

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

            companies.AsParallel().WithDegreeOfParallelism(3).ForAll(async company => {
                try
                {
                    #region Create EF connection

                    var sec = new UriSecurityContextProvider()
                    {
                        CompanyID = company.CompanyID,
                        ResourceID = 0,
                        CompanyPrefix = company.UrlPrefix,
                        IsAdministrator = true
                    };
                    var cache = new DummyCachingProvider();
                    var queue = new AzureQueueSource();
                    var community = new CommunityContext(cache, queue, sec);

                    #endregion

                    var rs = await community.UpdateRebuildJobStatus(CompanyRebuildJobToken.AssetGraph, CompanyRebuildJobStatusState.Active);
                    if (rs.StatusCode == System.Net.HttpStatusCode.OK)
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
                            finally 
                            {
                                await community.UpdateRebuildJobStatus(CompanyRebuildJobToken.AssetGraph, CompanyRebuildJobStatusState.Inactive);
                            }
                        }
                    }
                    
                    community.Dispose();
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
