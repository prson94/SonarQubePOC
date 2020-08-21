using d360.core;
using d360.core.entities.Metric;
using d360.core.queue;
using d360.extensions.storage;
using d360.utils.company;
using Dapper;
using igx.jobs.scoreprocessor.ChangeTypes;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
    public static class ScoringQueueProcessor
    {
        const string functionName = "Scoring_QueueProcessor";

#if DEBUG
        //public async static Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = true)]TimerInfo myTimer, System.Threading.CancellationToken token, TextWriter log)
        public async static Task Run([QueueTrigger("%ScoringQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
#else
        public async static Task Run([QueueTrigger("%ScoringQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
#endif

        {
#if DEBUG
            //var scoreInfo = new ScoreQueueInfo { ChangeType = ScoreQueueChangeType.ExternalMeasureResultsCreated, CompanyID = 2, ExecutionUid = Guid.Parse("526c115c-b66b-49bf-abca-192e24c9c1b4") };
            var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
#else
            var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
#endif

            try
            {
                AzureStorageProvider storage = new AzureStorageProvider();
                IScoreProcess process = null;

                switch (scoreInfo.ChangeType)
                {
                    case ScoreQueueChangeType.AssetMeasures:
                        process = new AssetMeasuresProcess();
                        break;
                    case ScoreQueueChangeType.ExternalMeasureResultsCreated:
                        process = new ExternalMeasureResultsCreatedProcess();
                        break;
                    case ScoreQueueChangeType.ExternalScoresCreated:
                        process = new ExternalScoresCreatedProcess();
                        break;
                    case ScoreQueueChangeType.MeasureChanged:
                        process = new MeasureChangedProcess();
                        break;
                    case ScoreQueueChangeType.MeasureCreated:
                        process = new MeasureCreatedProcess();
                        break;
                    case ScoreQueueChangeType.MeasureRemoved:
                        process = new MeasureRemovedProcess();
                        break;
                    case ScoreQueueChangeType.RollupPathChanged:
                        process = new RollupPathChangedProcess();
                        break;
                    case ScoreQueueChangeType.WorkflowCheck:
                        process = new WorkflowCheckProcess();
                        break;
                }

                if (process != null)
                {
                    process.Info = scoreInfo;
                    process.Storage = storage;
                    await process.Run();
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, scoreInfo.CompanyID);
                lock (log)
                {
                    log.WriteLine($"Company [{scoreInfo.CompanyID}]: [{ex.GetFullExceptionData()}]");
                }
                throw ex;
            }
        }
    }
}
