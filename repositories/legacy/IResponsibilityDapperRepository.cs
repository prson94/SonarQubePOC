using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core.entities;

namespace repositories
{
    public interface IResponsibilityDapperRepository
    {
        Task<IReadOnlyList<ResponsibilityBreakdownResponse>> GetResponsibilityTypeBreakdownAsync(Guid? responsibilityTypeUid);

        Task<IReadOnlyList<ResponsibilityBreakdownByResourceAggregate>> GetResponsibilityBreakdownByResourceAsync(Guid resourceUid, Guid? responsibilityTypeUid);
    }
}
