using Dapper;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    public class RollupPathChangedProcess : ProcessBase, IScoreProcess
    {
        public async Task Run()
        {            
            using (var company = GetEnvironmentConnection())
            {
                company.Open();

                ExecutionRecord = getExecution(company);

                await company.ExecuteAsync("exec metrics.CalculateRollups", commandTimeout: 600);

                updateExecution(company, ExecutionRecord, true, shouldDeleteAfterCompletion: true);
            }
        }
    }
}
