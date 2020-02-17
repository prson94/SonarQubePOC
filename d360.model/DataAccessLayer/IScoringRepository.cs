using d360.core.entities;
using d360.core.entities.Scoring;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IScoringRepository
    {
        void DeleteAllocation(ScoreTypeAllocation alloc);
        bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model);
        ScoreTypeAllocation GetAllocationByModel(AllocationApiUpsertModel model);
        ScoreTypeAllocation GetAllocationByUid(Guid allocationUid);
        List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams, out string error);
        bool HasActiveMeasures(ScoreTypeAllocation alloc);
        AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref ScoreTypeAllocation alloc);
        AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, ScoreTypeAllocation alloc);
        Task<List<AllocationApiGetUnallocatedAssetTypeModel>> GetUnallocatedAssetTypes(ScoreType scoreType);
        List<AssetTypeClass> AllowedClassesForScoreType();
        List<ExternalScoreResultsApiResultsModel> PostExternalResults(ScoreType scoreType, List<ExternalScoreResultsApiPostModel> model, ApiExecution execution);
        List<BulkMetricTemporaryTableModel> PostScoreResults(ScoreType scoreType, ApiExecution execution, List<ScoreResultApiPostModel> results);

    }
}