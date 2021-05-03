using d360.core.queue;
using System.Threading.Tasks;
using Dapper;
using System;
using d360.core.entities;
using System.Linq;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RollupPathChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            var model = await Storage.DeserializeJsonObjectFromBlobAsync<RollupPathChangedModel>(Info.StorageFolder, Info.StorageFile);

            if (model == null)
            {
                throw new ArgumentNullException("model","Cannot load score file from storage");
            }

            using (var company = GetEnvironmentConnection())
            {
                company.Open();

                var executionRecord = company.Query<ScoreExecution>("select * from metrics.Execution where Uid = @uid", new { uid = Info.ExecutionUid }).SingleOrDefault();

                if (executionRecord == null)
                {
                    throw new ArgumentNullException("executionRecord", "Execution record must exist.");
                }

                await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);

                updateExecution(company, executionRecord, true, shouldDeleteAfterCompletion: true);
            }
        }
    }
}
