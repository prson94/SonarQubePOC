using d360.core;
using d360.core.entities.Metric;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions.queue;
using d360.extensions.storage;
using d360.utils.company;
using Dapper;
using igx.jobs.scoreprocessor.ChangeTypes;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            //var scoreInfo = new ScoreQueueInfo { ChangeType = ScoreQueueChangeType.AssetMeasures, CompanyID = 59, StartedOn = DateTime.Parse("2021-07-01"), ExecutionUid = Guid.Parse("572C8380-512F-4361-9E57-EBC758470976") };
            var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
#else
            var scoreInfo = JsonConvert.DeserializeObject<ScoreQueueInfo>(myQueueItem);
#endif
            try
            {
                IScoreProcess process = null;

                switch (scoreInfo.ChangeType)
                {
                    case ScoreQueueChangeType.AssetMeasures:
                        process = new AssetMeasuresProcess();
                        break;
                    case ScoreQueueChangeType.CheckTypeDependencyRemoved:
                        process = new CheckTypeDependencyRemovedProcess();
                        break;
                    case ScoreQueueChangeType.MeasureChanged:
                        process = new MeasureChangedProcess();
                        break;
                    case ScoreQueueChangeType.MeasureRemoved:
                        process = new MeasureRemovedProcess();
                        break;
                    case ScoreQueueChangeType.RollupPathChanged:
                        process = new RollupPathChangedProcess();
                        break;
                    case ScoreQueueChangeType.RuleAssetRemoved:
                        process = new RuleAssetRemovedProcess();
                        break;
                    case ScoreQueueChangeType.WorkflowCheck:
                        process = new WorkflowCheckProcess();
                        break;
                }

                if (process != null)
                {
                    process.Info = scoreInfo;
                    await process.Run();
                }
            }
            catch (ArgumentNullException)
            {
                log.WriteLine($"No score execution record found. Company: {scoreInfo.CompanyID}; Execution: {scoreInfo.ExecutionUid}.");
            }
            catch (InvalidScoreMeasure ex)
            {
                var props = new Dictionary<string, string>() {
                        { "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
                        { "ChangeType", scoreInfo.ChangeType.ToString() }
                    };

                CoreFunction.AITrackException(functionName, ex, scoreInfo.CompanyID, props);
            }
            catch (ScoresCurrentlyProcessingException)
            {
                var queue = new AzureQueueSource();
                await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 0, 30));
                queue = null;
            }
            catch (Exception ex)
            {
                var props = new Dictionary<string, string>() {
                        { "ExecutionUid", scoreInfo.ExecutionUid.ToString() },
                        { "ChangeType", scoreInfo.ChangeType.ToString() }
                    };

                CoreFunction.AITrackException(functionName, ex, scoreInfo.CompanyID, props);

                var execUpdater = new ExecutionUpdater { Info = scoreInfo };
                var closedExecution = await execUpdater.UpdateAsync(ex);

                if (!closedExecution)
                {
                    var queue = new AzureQueueSource();
                    await queue.CreateMessageAsync(Config.GetValue<string>("ScoringQueue"), scoreInfo, new TimeSpan(0, 5, 0));
                    queue = null;
                }
            }
        }
    }
}
