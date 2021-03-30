using igx.jobs.scoreprocessor.ChangeTypes;
using System;
using Dapper;
using d360.core.entities;
using System.Data;
using System.Threading.Tasks;

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
                    if (company.State != ConnectionState.Open)
                        company.Open();

                    var exec = await company.QueryFirstOrDefaultAsync<ApiExecution>("select * from api.Execution where ExecutionID = @id", new { id = Info.ExecutionUid });
                    if (exec != null)
                    {
                        closedExecution = updateExecution(company, exec, false, ex);
                    }
                }
            }
            catch
            {
            }
            return closedExecution;
        }
    }
}
