using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class WorkflowCheckProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var scoreUids = await Storage.DeserializeJsonObjectFromBlobAsync<List<Guid>>(Info.StorageFolder, Info.StorageFile);
            await Task.Delay(100);
            //var Db = GetCompanyContext();

            // More work to do here. Sprint 9.
        }
    }
}
