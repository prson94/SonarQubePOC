using d360.core.entities;
using Dapper;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace igx.jobs.fusionloadprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            var host = new JobHost(config);
            host.RunAndBlock();
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
                    //log.Info($"Fusion Processing Took\tTIME ELAPSED {sw.ElapsedMilliseconds} MS");

                    CoreFunction.AITrackJobCompletedNoErrors(functionName);
                }
                catch (AggregateException exception)
                {
                    //log.Error("FusionQueueManager encountered and error while running fusion job.");
                    foreach (Exception ex in exception.InnerExceptions)
                    {
                        CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);

                        //log.Error($"Exception details [{ex.Message}]");
                    }
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);
                    //log.Error($"FusionQueueManager encountered and error while running fusion job.  Exception details [{ex.Message}]");
                }

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);
                //log.Error($"Company [{fusion.CompanyID}], Fusion ID [{fusion.FusionID}], LogFileName [{fusion.LogFileName}]: [{ex.GetFullExceptionData()}]");
            }

            CoreFunction.AIFlush();
        }

        static void executeWithTry(SqlConnection companyConnection, string lineageSql, int companyID, int timeout = 1200)
        {
            try
            {
                companyConnection.Execute(lineageSql, null, null, timeout);
            }
            catch (Exception ex)
            {
                //logger.Error(lineageSql);
                CoreFunction.AITrackException(functionName, ex, companyID);
                //logger.Error(ex.GetFullExceptionData());
            }
        }
    }
}
