using d360.core.queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RollupPathChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {
            var json = Storage.GetFileContentsAsString(Info.StorageFolder, Info.StorageFile);
            var model = JsonConvert.DeserializeObject<RollupPathChangedModel>(json);

            using (var company = GetEnvironmentConnection())
            {
                company.Open();
                await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);
            }
        }
    }
}
