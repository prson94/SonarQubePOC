using d360.core.entities.Scoring;
using System;
using System.Collections.Generic;

namespace d360.model.DataAccessLayer
{
    public interface IScoringRepository
    {
        void DeleteAllocation(ScoreTypeAllocation alloc);
        bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model);
        ScoreTypeAllocation GetAllocationByModel(AllocationApiUpsertModel model);
        ScoreTypeAllocation GetAllocationByUid(Guid allocationUid);
        List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams);
        bool HasActiveMeasures(ScoreTypeAllocation alloc);
        AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref ScoreTypeAllocation alloc);
        AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, ScoreTypeAllocation alloc);
    }
}