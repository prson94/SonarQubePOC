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

#if DEBUG
        public static async Task Run([TimerTrigger("0 0 1 * * *", RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
#else
        public static async Task Run([QueueTrigger("%AssetGraphQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
#endif
        {
            RebuildAssetGraphModel queueInfo = null;
#if DEBUG
            queueInfo = new RebuildAssetGraphModel { CompanyID = 4, To = 0 };
#else
            queueInfo = JsonConvert.DeserializeObject<RebuildAssetGraphModel>(myQueueItem);
#endif
            using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(queueInfo.CompanyID))
            {
                const int timeout = 60 * 180;

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

#if DEBUG
            CoreFunction.AITrackJobCompletedNoErrors(functionName);
            CoreFunction.AIFlush();
#endif

        }
    }
}
