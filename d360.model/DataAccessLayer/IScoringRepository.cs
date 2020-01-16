using d360.core.entities.Scoring;
using System;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer
{
    public interface IScoringRepository
    {
        void DeleteAllocation(Allocation alloc);
        bool DoesAllocationExists(Guid allocationUid, AllocationApiUpsertModel model);
        Allocation GetAllocationByModel(AllocationApiUpsertModel model);
        Allocation GetAllocationByUid(Guid allocationUid);
        List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool HasActiveMeasures(Allocation alloc);
        AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref Allocation alloc);
        AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, Allocation alloc);
    }
}