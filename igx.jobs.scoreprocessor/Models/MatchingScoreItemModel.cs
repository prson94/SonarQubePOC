using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class MatchingScoreItemModel
    {
        public Guid ScoreItemUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
    }
}
