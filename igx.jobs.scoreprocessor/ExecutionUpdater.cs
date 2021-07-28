using igx.jobs.scoreprocessor.ChangeTypes;
using System;
using Dapper;
using d360.core.entities;
using System.Data;
using System.Threading.Tasks;
using d360.model;

namespace igx.jobs.scoreprocessor
{
    public class ExecutionUpdater: ProcessBase
    {
        public async Task<bool> UpdateAsync(Exception ex)
        {
            bool closedExecution = false;
            try
            {
                using (var company = GetEnvironmentConnection())
                {
                    await company.OpenIfClosed();

                    var exec = await company.QueryFirstOrDefaultAsync<ScoreExecution>("select * from metrics.Execution where Uid = @id", new { id = Info.ExecutionUid });
                    if (exec != null)
                    {
                        closedExecution = updateExecution(company, exec, false, ex);
                    }
                }
            }
            catch
            {
                // This is solely to log the exception on the execution record. We should not fail the job for this.
            }
            return closedExecution;
        }
    }
}
