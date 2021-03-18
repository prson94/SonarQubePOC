using d360.core.entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusionloadprocessor
{
    class Program
    {
        static async Task Main()
        {
            using (var host = CoreFunction.JobHostConfigBuilder().Build())
            {
                await host.RunAsync();
            }
        }
    }

    public static class FusionLoadProcessor
    {
        const string functionName = "Fusion_ProcessLoad";

        public static async Task Run([QueueTrigger("%FusionLoadQueue%"), StorageAccount("QueueStorageAccount")]string myQueueItem, TextWriter log)
        {
            CoreFunction.AITrackJobStart(functionName);

            var fusion = JsonConvert.DeserializeObject<FusionProcessingData>(myQueueItem);
            var fp = new FusionProcessor();

            try
            {
                var bulkTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBBulkCopyTimeout"));
                var readTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBReadQueryTimeout"));
                var executionTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBExecuteQueryTimeout"));
                var mergeSize = int.Parse(CoreFunction.GetConfigValueByKey("MergeChunkSize"));

                try
                {
                    var sw = Stopwatch.StartNew();
                    await fp.Process(functionName, fusion, bulkTimeout, readTimeout, executionTimeout, mergeSize, log);
                    
                    CoreFunction.AITrackJobCompletedNoErrors(functionName);
                }
                catch (AggregateException exception)
                {                    
                    foreach (Exception ex in exception.InnerExceptions)
                    {
                        CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);                    
                }

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);                
            }

            CoreFunction.AIFlush();
        }
    }
}
