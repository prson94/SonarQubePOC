using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AssetAllocationPreviousResult
    {
        public Guid ScoreUid { get; set; }

        public Guid ScoreItemUid { get; set; }

        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public Guid? ConditionUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool UsedInOtherScores { get; set; }
        public bool Value { get; set; }
        public decimal AdjustedWeight { get; set; }
        public decimal AdjustedMaxWeight { get; set; }
        public float? DecimalValue { get; set; }
        public string OtherConditions { get; set; }
        public string Evidence { get; set; }
    }
}
