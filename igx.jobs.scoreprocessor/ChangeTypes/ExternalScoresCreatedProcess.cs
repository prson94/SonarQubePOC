using d360.core.entities.Metric;
using d360.core.queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class ExternalScoresCreatedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var json = Storage.GetFileContentsAsString(Info.StorageFolder, Info.StorageFile);
            var scoreUids = JsonConvert.DeserializeObject<List<Guid>>(json);

            var Db = GetCompanyContext();

            // More work to do here. Sprint 9.
            await Task.Delay(10);
            Db.SendScoreEventWithPayload(Guid.NewGuid(), ScoreQueueChangeType.WorkflowCheck, scoreUids);
        }
    }
}
