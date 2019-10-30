using System;
using System.Collections.Generic;
using d360.core.entities;
using d360.core.entities.Metric;

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
        MetricAssetHierarchyModels GetMetricHierarchyByAsset(Guid assetUid, DateTime? effectiveDate);
        List<string> GetMetricStructureFragments(Guid assetTypeUid);
        MetricScoreApiModel GetMetricScore(AssetType at, IEnumerable<KeyValuePair<string, string>> queryParams);
    }
}