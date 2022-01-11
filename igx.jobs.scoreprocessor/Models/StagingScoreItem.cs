using System;
using System.Collections.Generic;

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
        public bool IsRemoved { get; set; }
        public string Evidence { get; set; }
        public bool HasThreshold { get; set; } = false;
        public string OtherConditions { get; set; }
    }

    public class StagingScoreItemComparer : IEqualityComparer<StagingScoreItem>
    {
        public bool Equals(StagingScoreItem x, StagingScoreItem y)
        {
            if (x.AssetUid == y.AssetUid && x.EffectiveDate == y.EffectiveDate && x.MeasureVersionUid == y.MeasureVersionUid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetHashCode(StagingScoreItem obj)
        {
            int hCode = new { AssetUid = obj.AssetUid, EffectiveDate = obj.EffectiveDate, MeasureVersionUid = obj.MeasureVersionUid }.GetHashCode();
            return hCode.GetHashCode();
        }
    }
}
