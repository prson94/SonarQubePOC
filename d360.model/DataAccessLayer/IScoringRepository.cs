using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;

namespace d360.model.DataAccessLayer
{
    public interface IScoringRepository
    {
        void DeleteAllocation(MetricAllocation alloc);
        
        bool DoesAllocationExist(Guid allocationUid, AllocationApiUpsertModel model);
        
        MetricAllocation GetAllocationByModel(AllocationApiUpsertModel model);
        
        MetricAllocation GetAllocationByUid(Guid allocationUid);
        
        List<AllocationApiGetModel> GetAllocations(IEnumerable<KeyValuePair<string, string>> queryParams, out string error, AssetTypeClass? Class = null);
        
        Task<DataQualityScoreItemEvidenceViewModel> GetEvidenceForDataQualityScoreItem(Guid scoreItemUid, IEnumerable<KeyValuePair<string, string>> queryParams);
        
        bool HasActiveMeasures(MetricAllocation alloc);
        
        AllocationApiGetModel PostAllocation(AllocationApiUpsertModel model, ref MetricAllocation alloc);
        
        AllocationApiGetModel UpdateAllocation(AllocationApiUpsertModel model, MetricAllocation alloc);
        
        Task<List<AllocationApiGetUnallocatedAssetTypeModel>> GetUnallocatedAssetTypes(ScoreType scoreType);
        
        List<AssetTypeClass> AllowedClassesForScoreType();
        
        List<ExternalScoreResultApiResponseModel> PostExternalResults(MetricAllocation allocation, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution);
        
        List<ExternalScoreResultApiResponseModel> PostExternalResults(ScoreType scoreType, List<ExternalScoreResultApiRequestModel> model, ApiExecution execution);
        
        List<InternalScoreResultApiResponseModel> PostScoreResults(ScoreType scoreType, ApiExecution execution, List<InternalScoreResultApiRequestModel> results);
        
        List<InternalScoreResultApiResponseModel> PostScoreResults(MetricAllocation allocation, ApiExecution execution, List<InternalScoreResultApiRequestModel> results);
        
        ScoreExecution GetExecutionById(Guid uid);
        
        IQueryable<ScoreExecution> GetExecutions(int pageSize, int pageNumber);
        
        List<ScoreExecutionItemViewModel> GetExecutionItems(long executionId, int pageSize, int pageNumber, ScoreQueueChangeType? changeType = null);
    }
}
