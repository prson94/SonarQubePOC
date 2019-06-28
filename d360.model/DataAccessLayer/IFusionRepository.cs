
using d360.core.entities;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IFusionRepository
    {
        Asset GetFusionByUID(Guid guid);
        bool HasFusionRules(int fusionId);
        Task<ApiExecutionInfo> BulkDeleteFusionConfiguration(Guid assetUid, bool Cascade, ApiExecution execution);
    }
}
