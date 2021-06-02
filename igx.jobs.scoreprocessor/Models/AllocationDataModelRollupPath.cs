using System;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModelRollupPath
    {
        public Guid AssetVersionRollupPathUid { get; set; }
        public string FilterMatchType { get; set; }
        public List<AllocationDataModelRollupPathSegmentLink> SegmentLinks { get; set; }
        public List<AllocationDataModelRollupPathFilter> Filters { get; set; }
    }
}
