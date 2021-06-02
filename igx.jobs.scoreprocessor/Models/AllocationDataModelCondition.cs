using d360.core.enums;
using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModelCondition
    {
        public Guid ConditionUid { get; set; }
        public MetricMatchType MatchType { get; set; }
        public int Position { get; set; }
        public decimal Weight { get; set; }
        public float Threshold { get; set; }
        public List<AllocationDataModelConditionItem> Items { get; set; }
    }
}
