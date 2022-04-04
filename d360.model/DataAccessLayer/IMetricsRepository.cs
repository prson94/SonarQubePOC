using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.enums;
using d360.core.queue;

namespace d360.model.DataAccessLayer
{
    public interface IMetricsRepository
    {
        WorkHttpStatus AddOrUpdateMetrics(MetricAssetEditModel model);
        
        void DeleteMetric(MetricAsset model);
        
        MetricAsset GetActiveMetric(Guid uid);
        
        Task<List<MetricFieldTypeViewModel>> GetFieldsByRuleResultPath(Guid ruleResultPathUid);
        
        MetricAsset GetMetricByUid(Guid uid);
        
        MetricAssetViewDetailModel GetMetricViewModelByUid(Guid uid, DateTime? effectiveDate);
        
        [Obsolete]
        MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate);
        
        List<MetricFieldTypeViewModel> GetMetricConditionsFields(Guid assetTypeUid);
        
        List<RootMetricAssetHierarchyModel> GetMetricHierarchyByAsset(Guid allocationUid, Guid assetUid, DateTime? effectiveDate);
        
        Task<IEnumerable<MetricPathOptionViewModel>> GetMetricPathOptionsBy(int assetTypeId, ScoreType scoreType);
        
        List<MetricAssetViewModel> GetMetricStructureByAllocation(Guid allocationUid, List<State> states);
        
        (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams);
        
        DataQualityGetResultModel GetDataQualityResults(Guid owningAssetUid, Guid? v, int pageSize, int pageNum, string sort, string direction, DateTime? effectiveDateStart, DateTime? effectiveDateEnd, bool includeDuplicateFlag = false, string _filter = "", string _simpeFilter = "");
        
        List<DataQualityResponseModel> InsertDataQualityResult(List<DataQualityInsertModel> request, ApiExecution execution);
        
        List<DataQualityResponseModel> UpdateDataQualityResult(List<DataQualityUpdateModel> request, ApiExecution execution);
        
        List<DataQualityAssetResultModel> GetAssetResultDetailsByUid(Guid value);
        
        List<DataQualityDeleteResponseModel> DeleteDataQualityResult(List<DataQualityDeleteModel> list, ApiExecution execution);
        
        Task<ApiExecutionInfo> PostBulkDataQualityResults(List<DataQualityInsertModel> request, ApiExecution execution, bool sendWorkflowEvents = true);
        
        List<MeasureVersionHistoryModel> GetMetricVersionHistory(Guid measureUid);
        
        Guid RecalculateMeasureScoreItems(Guid allocationUid, Guid measureUid);
    }
}
