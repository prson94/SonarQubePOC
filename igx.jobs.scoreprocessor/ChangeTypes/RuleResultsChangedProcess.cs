using d360.core.queue;
using Dapper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RuleResultsChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            await Task.Delay(1);

            var json = Storage.GetFileContentsAsString(Info.StorageFolder, Info.StorageFile);
            var ruleResultUids = JsonConvert.DeserializeObject<List<Guid>>(json);

            
        }
    }
}
