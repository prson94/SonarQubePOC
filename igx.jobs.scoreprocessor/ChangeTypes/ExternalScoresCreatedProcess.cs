using d360.core.entities.Metric;
using d360.core.queue;
using System;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class ExternalScoresCreatedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var scores = await Storage.DeserializeJsonObjectFromBlobAsync<List<ScoreCreatedModel>>(Info.StorageFolder, Info.StorageFile);

            var Db = GetCompanyContext();

            // More work to do here. Sprint 9.
            await Task.Delay(10);
            Db.SendScoreEventWithPayload(Info.ExecutionUid, ScoreQueueChangeType.WorkflowCheck, scores, Info.StartedOn);
        }
    }
}
