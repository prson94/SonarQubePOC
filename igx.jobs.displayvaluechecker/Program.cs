using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using d360.core.enums;
using Microsoft.Extensions.Hosting;

namespace igx.jobs.displayvaluechecker
{
    class Program
    {
        static async Task Main()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;

            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage()
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class DisplayValueChecker
    {
        const string functionName = "DisplayValueChecker";

#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 0 */6 * * *"; // every 6 hours
#endif

        public static async Task Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                foreach (var c in companies)
                {
                    var company = JobDbContextCreator.CreateCompanyContext(c.CompanyID, 0, c.UrlPrefix, true);
                    try
                    {
                        var rs = await company.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Active);
                        if (rs.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            try
                            {
                                using (var companyConn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                                {
                                    companyConn.Open();
                                    await companyConn.ExecuteAsync("CheckDisplayValues", commandTimeout: 600);
                                }
                            }
                            catch (Exception ex)
                            {
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            }
                            finally
                            {
                                await company.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Inactive);
                            }
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
