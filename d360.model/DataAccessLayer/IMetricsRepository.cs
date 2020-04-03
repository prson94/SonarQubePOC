using System;
using System.Collections.Generic;
using d360.core.entities;
using d360.core.entities.Metric;
using d360.core.entities.Scoring;
using d360.core.enums;

namespace d360.model.DataAccessLayer
{
    public interface IMetricsRepository
    {
        WorkHttpStatus AddOrUpdateMetrics(MetricAssetViewModel model, out bool isNew);
        List<BulkMetricTemporaryTableModel> BulkMetricsImport(BulkMetricsImport model, ApiExecution execution);
        void DeleteMetric(MetricAsset model);
        MetricAsset GetActiveMetric(Guid uid);
        MetricAsset GetMetricByUid(Guid uid);
        MetricAssetTypeHierarchyModels GetMetricDefinitionHierarchyByAssetType(Guid assetTypeUid, DateTime? effectiveDate);
        List<string> GetMetricFieldFragments(Guid assetTypeUid);
        MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid assetUid, DateTime? effectiveDate, ScoreType scoreType);
        List<int> GetScoreTypesForAsset(Guid assetUid);
        List<string> GetMetricStructureFragments(Guid assetTypeUid, ScoreType scoreType);
        ScoreTypeAllocation GetAllocationByMetricModel(MetricAssetViewModel model);
        (MetricScoreApiModel, string) GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams);
        DataQualityResult GetDataQualityResults(Guid owningAssetUid, Guid? v, int pageSize, int pageNum, string sort, string direction, DateTime? effectiveDateStart, DateTime? effectiveDateEnd);
        List<DataQualityResponseModel> InsertDataQualityResult(List<DataQualityInsertModel> request, ApiExecution execution);
    }
}