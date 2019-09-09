using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.utils.company;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Newtonsoft.Json;
using d360.core.queue;

namespace igx.jobs.assetgraphprocessor
{
    public class RebuildRequestQueueProcessor
    {
        const string functionName = "AssetGraphProcessor_RebuildQueueRequest";

        public static async Task Run([QueueTrigger("%AssetGraphQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var queueInfo = JsonConvert.DeserializeObject<RebuildAssetGraphModel>(myQueueItem);

            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(queueInfo.CompanyID))
            {
                string lineageVersion = CompanyConnectionUtils.GetCompanySettings(queueInfo.CompanyID).FirstOrDefault(s => s.SettingID == 68)?.Value ?? "";

                if (lineageVersion == "3")
                {
                    const int timeout = 1000 * 60 * 10;

                    companyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    try
                    {
                        await companyConnection.ExecuteAsync("graph.SynchronizeTables @populatePaths", new { populatePaths = true }, commandTimeout: timeout);
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, queueInfo.CompanyID);
                    }
                }
            }

            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
        }
    }
}
