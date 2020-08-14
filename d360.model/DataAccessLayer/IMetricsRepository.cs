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
        WorkHttpStatus AddOrUpdateMetrics(MetricAssetViewModel model, out bool isNew);
        List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution);
        void DeleteMetric(MetricAsset model);
        MetricAsset GetActiveMetric(Guid uid);
        MetricAsset GetMetricByUid(Guid uid);
        MetricAssetViewDetailModel GetMetricViewModelByUid(Guid uid, DateTime? effectiveDate);
        MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate);
        List<string> GetMetricFieldFragments(Guid assetTypeUid);
        MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid assetUid, DateTime? effectiveDate, ScoreType scoreType);
        Task<IEnumerable<MetricPathOptionViewModel>> GetMetricPathOptionsBy(int assetTypeId, ScoreType scoreType);
        List<int> GetScoreTypesForAsset(Guid assetUid);
        List<string> GetMetricStructureFragments(Guid allocationUid);
        MetricAllocation GetAllocationByMetricModel(MetricAssetViewModel model);
        (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams);
        DataQualityGetResultModel GetDataQualityResults(Guid owningAssetUid, Guid? v, int pageSize, int pageNum, string sort, string direction, DateTime? effectiveDateStart, DateTime? effectiveDateEnd, bool includeDuplicateFlag = false);
        List<DataQualityResponseModel> InsertDataQualityResult(List<DataQualityInsertModel> request, ApiExecution execution);
        List<DataQualityResponseModel> UpdateDataQualityResult(List<DataQualityUpdateModel> request, ApiExecution execution);
        List<DataQualityAssetResultModel> GetAssetResultDetailsByUid(Guid value);
        List<DataQualityDeleteResponseModel> DeleteDataQualityResult(List<DataQualityDeleteModel> list, ApiExecution execution);
        Task<ApiExecutionInfo> PostBulkDataQualityResults(List<DataQualityInsertModel> request, ApiExecution execution, bool sendWorkflowEvents = true);
        List<string> GetMetricVersionHistory(Guid measureUid);
    }
}