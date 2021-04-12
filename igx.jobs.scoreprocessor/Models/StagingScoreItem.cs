using System;

namespace igx.jobs.scoreprocessor.Models
{
    public class StagingScoreItem
    {
        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public Guid MeasureUid { get; set; }
        public Guid MeasureVersionUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool Value { get; set; }
        public float? DecimalValue { get; set; }
        public decimal? RawWeight { get; set; }
        public Guid? ConditionUid { get; set; }
        public char Actino { get; set; }
        public string Evidence { get; set; }
        public string OtherConditions { get; set; }
    }
}
