using d360.core.queue;
using System.Threading.Tasks;
using Dapper;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RollupPathChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var model = await Storage.DeserializeJsonObjectFromBlobAsync<RollupPathChangedModel>(Info.StorageFolder, Info.StorageFile);

            using (var company = GetEnvironmentConnection())
            {
                company.Open();
                await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);
            }
        }
    }
}
