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
            var scoreUids = await Storage.DeserializeJsonObjectFromBlobAsync<List<Guid>>(Info.StorageFolder, Info.StorageFile);

            var Db = GetCompanyContext();

            // More work to do here. Sprint 9.
            await Task.Delay(10);
            Db.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.WorkflowCheck, scoreUids);
        }
    }
}
