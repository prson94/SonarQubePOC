using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.utils.company;
using Dapper;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using d360.core.enums;
using System.Collections.Generic;

namespace igx.jobs.assetgraphprocessor
{
    public class SynchronizeTables
    {
        const string functionName = "AssetGraphProcessor_SynchronizeTables";
#if DEBUG
        const string timerSettings = "*/2 * * * * *";
#else        
        const string timerSettings = "0 0 0 * * 6";
#endif

        public static async Task RunSyncTables([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
#if DEBUG
            var companies = CoreFunction.GetCompaniesByCurrentSlot().Where(i => i.CompanyID == 1).ToList();
#else
            var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

            var populatePaths = DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday;

            CoreFunction.AITrackJobStart(functionName);

            companies.AsParallel().WithDegreeOfParallelism(3).ForAll(async company => {
                try
                {
                    CoreFunction.AITrackEvent(functionName, "Graph Rebuild", new Dictionary<string, string>() { { "PopulatePaths", populatePaths.ToString() } }, company.CompanyID);
                    CoreFunction.AIFlush();

                    var companyCtx = JobDbContextCreator.CreateCompanyContext(company.CompanyID, 0, company.UrlPrefix, true);

                    var rs = await companyCtx.UpdateRebuildJobStatus(CompanyRebuildJobToken.AssetGraph, CompanyRebuildJobStatusState.Active);
                    if (rs.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var conn = CompanyConnectionUtils.GetCompanyConnection(company.CompanyID, company.Server, company.Username, company.Password);

                        using (conn)
                        {
                            const int timeout = 60 * 360; // 6 hours

                            try
                            {
                                conn.Open();
                                await conn.ExecuteAsync("graph.SynchronizeTables @populatePaths", new { populatePaths }, commandTimeout: timeout);
                            }
                            catch (Exception ex)
                            {
                                CoreFunction.AITrackException(functionName, ex, company.CompanyID);
                            }
                            finally
                            {
                                await companyCtx.UpdateRebuildJobStatus(CompanyRebuildJobToken.AssetGraph, CompanyRebuildJobStatusState.Inactive);
                            }
                        }
                    }

                    companyCtx.Dispose();
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, company.CompanyID);
                }
            });

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
