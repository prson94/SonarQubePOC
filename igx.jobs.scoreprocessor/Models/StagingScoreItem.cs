using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.Models
{
    public class StagingScoreItem
    {
        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public Guid MeasureUid { get; set; }
        public Guid? ParentMeasureUid { get; set; }
        public Guid MeasureVersionUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public char Action { get; set; }
        public bool Value { get; set; }
        public float? DecimalValue { get; set; }
        public decimal? RawWeight { get; set; }
        public decimal? AdjustedWeight { get; set; }
        public decimal? AdjustedMaxWeight { get; set; }
        public DateTime? RunDate { get; set; }
        public DateTime UpdatedOn { get; set; }
        public Guid? ConditionUid { get; set; }
        public string Evidence { get; set; }
        public string OtherConditions { get; set; }

        public Guid? ScoreUid { get; set; }
        public Guid? ScoreItemUid { get; set; }
        public DateTime? PastEffectiveDate { get; set; }
    }
}
