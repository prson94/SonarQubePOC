using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace d360.core.queue
{
    public enum ScoreQueueChangeType
    {
        AssetMeasures,
        ExternalMeasureResultsCreated,
        ExternalScoresCreated,
        MeasureCreated,
        MeasureChanged,
        MeasureRemoved,
        RuleResultsCreated,
        RuleResultsChanged,
        RuleResultsRemoved,
        WorkflowCheck
    }

    public enum ScoreQueueExecutionDataLocation
    {
        File,
        Table
    }

    public class ScoreQueueInfo
    {
        public int CompanyID { get; set; }

        public Guid ExecutionUid { get; set; }
        
        public ScoreQueueExecutionDataLocation Location { get; set; }
        
        public ScoreQueueChangeType ChangeType { get; set; }

        [JsonIgnore]
        public string StorageFolder { get { return $"scoring"; } }

        [JsonIgnore]
        public string StorageFile { get { return $"{CompanyID}/{ExecutionUid}.json"; } }
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
}
