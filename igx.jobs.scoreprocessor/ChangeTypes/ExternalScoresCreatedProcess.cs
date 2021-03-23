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

            if (scores == null)
            {
                throw new ArgumentNullException("scores","Cannot load score file from storage");
            }

            var Db = GetCompanyContext();

            // More work to do here. Sprint 9.
            await Task.Delay(10);

            await Db.SendContinuingScoreEventWithPayload(ScoreQueueChangeType.WorkflowCheck, scores, Info.ExecutionUid, Info.StartedOn);
        }
    }
}
