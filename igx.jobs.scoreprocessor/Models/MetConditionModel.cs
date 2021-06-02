using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class MetConditionModel
    {
        public int Position { get; set; }
        public Guid ConditionUid { get; set; }
        public decimal? Weight { get; set; }
        public float? Threshold { get; set; }

    }
}
