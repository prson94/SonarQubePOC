using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;

namespace d360.model
{
    public partial class CompanyContext : BaseContext
    {
        public DbSet<CompanyRebuildJobStatus> RebuildJobStatuses { get; set; }

        public async Task<List<CompanyRebuildJobStatus>> GetRebuildJobStatuses(int timeOutInHours)
        {
            List<CompanyRebuildJobStatus> list = await RebuildJobStatuses.ToListAsync();
            list.ForEach(i =>
            {
                if (i.State == CompanyRebuildJobStatusState.Active && i.LastStartedOn <= DateTime.UtcNow.AddHours(-timeOutInHours))
                {
                    i.State = CompanyRebuildJobStatusState.Inactive;
                }
            });

            return list;
        }

        public async Task<CompanyRebuildJobStatusState> GetRebuildJobStatus(CompanyRebuildJobToken jobToken, int timeOutInHours)
        {
            CompanyRebuildJobStatus status = await RebuildJobStatuses.FirstOrDefaultAsync(j => j.JobToken == jobToken);
            CompanyRebuildJobStatusState state = CompanyRebuildJobStatusState.Inactive;

            if (status != null && status.LastStartedOn > DateTime.UtcNow.AddHours(-timeOutInHours))
            {
                state = status.State;
            }

            return state;
        }

        public async Task<WorkHttpStatus> UpdateRebuildJobStatus(CompanyRebuildJobToken jobToken, CompanyRebuildJobStatusState state, int timeOutInHours)
        {
            CompanyRebuildJobStatus status = await RebuildJobStatuses.FirstOrDefaultAsync(j => j.JobToken == jobToken);
            WorkHttpStatus returnValue = null;

            if (status != null)
            {
                if (status.State == CompanyRebuildJobStatusState.Active && status.LastStartedOn > DateTime.UtcNow.AddHours(-timeOutInHours) && state == CompanyRebuildJobStatusState.Active)
                {
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.Conflict, OthersError.JobIsRunning, OthersError.JobinActiveState);
                }
                else
                {
                    status.State = state;

                    if (state == CompanyRebuildJobStatusState.Active)
                    {
                        status.LastStartedOn = DateTime.UtcNow;
                        status.LastCompletedOn = null;
                    }
                    else
                    {
                        status.LastCompletedOn = DateTime.UtcNow;
                    }

                    Update(status);
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");
                }
            }
            else
            {
                if (state == CompanyRebuildJobStatusState.Inactive)
                {
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.Conflict, OthersError.JobIsNotRunning, OthersError.JobIsNotRunning);
                }
                else
                {
                    status = new CompanyRebuildJobStatus { JobToken = jobToken, LastStartedBy = CurrentResourceID, LastStartedOn = DateTime.UtcNow, State = state };
                    Add(status);
                    returnValue = new WorkHttpStatus(System.Net.HttpStatusCode.OK, "", "");
                }
            }

            return returnValue;
        }
    }
}
