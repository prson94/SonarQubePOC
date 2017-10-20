using d360.core;
using d360.core.entities;
using Dapper;
using igx.functions.Core;
using igx.functions.Queue.fusion;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace igx.functions.Queue
{
    public static class ProcessFusionLoad
    {
        const string functionName = "ProcessFusionLoad";

        [FunctionName(functionName)]
        public static void Run([QueueTrigger("%FusionLoadQueue%", Connection = "MainStorageAccount")]string myQueueItem, TraceWriter log)
        {
            var fusion = JsonConvert.DeserializeObject<FusionProcessingData>(myQueueItem);
            var fp = new FusionProcessor();

            try
            {
                var bulkTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBBulkCopyTimeout"));
                var readTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBReadQueryTimeout"));
                var executionTimeout = int.Parse(CoreFunction.GetConfigValueByKey("DBExecuteQueryTimeout"));
                var mergeSize = int.Parse(CoreFunction.GetConfigValueByKey("MergeChunkSize"));

                var t = Task.Run(async delegate
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        await fp.Process(fusion, bulkTimeout, readTimeout, executionTimeout, mergeSize);
                        Trace.TraceInformation(string.Format("Fusion Processing Took\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));
                    }
                    catch (AggregateException exception)
                    {
                        Trace.TraceError("FusionQueueManager encountered and error while running fusion job.");
                        foreach (Exception ex in exception.InnerExceptions)
                            Trace.TraceError("Exception details [{0}]", ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("FusionQueueManager encountered and error while running fusion job.  Exception details [{0}]", ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, fusion.CompanyID);
                log.Error($"Company [{fusion.CompanyID}], Fusion ID [{fusion.FusionID}], LogFileName [{fusion.LogFileName}]: [{ex.GetFullExceptionData()}]");
            }
        }

        static void executeWithTry(SqlConnection companyConnection, TraceWriter logger, string lineageSql, int companyID, int timeout = 1200)
        {
            try
            {
                companyConnection.Execute(lineageSql, null, null, timeout);
            }
            catch (Exception ex)
            {
                logger.Error(lineageSql);
                CoreFunction.AITrackException(functionName, ex, companyID);
                logger.Error(ex.GetFullExceptionData());
            }
        }
    }
}
