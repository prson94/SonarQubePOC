using d360.utils.company;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using d360.core.enums;
using Microsoft.Azure;
using System.Threading;
using Microsoft.Azure.Storage.Blob;

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
				//                companies = companies.Where(x => x.CompanyID == 1).ToList();
#endif

				foreach (var c in companies)
                {
					// Clear old blob files.
					await RemoveOldBlobs("api-execution", c.CompanyID);
					await RemoveOldBlobs("bulk-loads", c.CompanyID, 60);
					await RemoveOldBlobs("scoring", c.CompanyID);

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

		static async Task RemoveOldBlobs(string container, int companyId, int days = 30)
		{
			try
			{
				var acct = StorageAccount.NewFromConnectionString(CloudConfigurationManager.GetSetting("MainStorageAccount"));
				var blobClient = acct.CreateCloudBlobClient();
				var token = new BlobContinuationToken();
				var path = $"{container}/{companyId}/";

				var blobsResult = await blobClient.ListBlobsSegmentedAsync(path, token);
				foreach (var blob in blobsResult.Results)
				{
					CloudBlockBlob bl = (CloudBlockBlob)blob;
					TimeSpan? diff = DateTime.Today - bl.Properties.LastModified;
					if (diff?.Days > days)
					{
						await bl.DeleteAsync();
					}
				}
			}
			catch (Exception ex)
			{
				CoreFunction.AITrackException(functionName, ex);
			}
		}
    }
}
