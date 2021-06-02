using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace d360.core.queue
{
    public enum ScoreQueueChangeType
    {
        AssetMeasures = 0,
        MeasureChanged = 3,
        MeasureRemoved = 4,
        RollupPathChanged = 5,
        WorkflowCheck = 6,
        CheckTypeDependencyRemoved = 7,
        RuleAssetRemoved = 8,
        ResponsibilityRuleProcessed = 9
    }

    public class ScoreQueueInfo
    {
        public int CompanyID { get; set; }
        public int? ResourceID { get; set; }

        public Guid ExecutionUid { get; set; }

        public DateTime StartedOn { get; set; }

        public ScoreQueueChangeType ChangeType { get; set; }

        [JsonIgnore]
        private string StartedOnDateString { get { return StartedOn.ToString("yyyyMMdd"); } }

        [JsonIgnore]
        public string StorageFolder { get { return $"scoring"; } }

        [JsonIgnore]
        public string StorageFile { get { return $"{StorageFilePrefix}.json"; } }

        [JsonIgnore]
        public string StorageFilePrefix { get { return $"{CompanyID}/{StartedOnDateString}_{ExecutionUid}_{ChangeType}"; } }
    }

    public class CheckTypeDependencyRemovedModel
    {
        public List<Guid> VersionUids { get; set; }
    }

    public class ExternalMeasureResultsCreatedModel
    {
        public Guid AssetUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool Result { get; set; }

        // Populated in processing job.
        public Guid? MetricAssetVersionUid { get; set; }
        public Guid? AllocationUid { get; set; }
        public int? AssetTypeId { get; set; }
    }

    public class RollupPathChangedModel
    {
        public int? IntersectTypeId { get; set; }
        public int? AssetTypeId { get; set; }
    }

    public class RuleAssetRemovedModel
    {
        public Guid AssetUid { get; set; }
    }
}
