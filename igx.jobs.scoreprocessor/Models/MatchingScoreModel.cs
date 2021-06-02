using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class MatchingScoreModel
    {
        public Guid ScoreUid { get; set; }
        public Guid AllocationUid { get; set; }
        public Guid AssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string VersionValueHash { get; set; }
    }
}
