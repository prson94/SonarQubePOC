using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using d360.core.enums;

namespace igx.jobs.databasecleaner
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder
            .ConfigureWebJobs(c =>
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

    public static class DatabaseCleaner
    {
        const string functionName = "DatabaseMaintenance_Cleaner";

#if DEBUG
        const string timerSettings = "*/1 * * * * *";
#else
        const string timerSettings = "0 0 4 * * *";
#endif

        public static async Task Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
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
                            string overrideValue = company.Query<string>("select Value from Setting where ID = @ID", new { ID = (int)Setting.AssetDataProfileLifespan }).SingleOrDefault();
                            var settingInfo = Setting.AssetDataProfileLifespan.AsInfoModel();
                            settingInfo.Value = (string.IsNullOrEmpty(overrideValue)) ? settingInfo.DefaultValue : overrideValue;

                            //remove any old api execution records
                            await company.ExecuteAsync("[api].[DeleteExecutionRecords]", commandTimeout: 1800);

                            //remove any old data profile records
                            await company.ExecuteAsync("[DeleteAssetDataProfileRecords] @dataProfileLifespan", new { dataProfileLifespan = (int)(Convert.ChangeType(settingInfo.Value, typeof(int))) }, commandTimeout: 1800);

                            //remove any old score execution data
                            await company.ExecuteAsync("metrics.CleanupExecutions", commandTimeout: 1800);

                            //remove any old queue task data
                            await company.ExecuteAsync("Queue.DeleteQueueTaskRecords", commandTimeout: 1800);

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
