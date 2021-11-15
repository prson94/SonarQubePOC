using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using d360.core.entities;

namespace d360.model.DataAccessLayer
{
    public interface IResponsibilityDapperRepository
    {
        Task<IReadOnlyList<ResponsibilityBreakdownResponse>> GetResponsibilityTypeBreakdownAsync(Guid? typeUid);
    }
}