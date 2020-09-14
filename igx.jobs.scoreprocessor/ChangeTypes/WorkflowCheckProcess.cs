using d360.core.entities.Metric;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class WorkflowCheckProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var scores = await Storage.DeserializeJsonObjectFromBlobAsync<List<ScoreCreatedModel>>(Info.StorageFolder, Info.StorageFile);
            await Task.Delay(100);
            //var Db = GetCompanyContext();
        }
    }
}
