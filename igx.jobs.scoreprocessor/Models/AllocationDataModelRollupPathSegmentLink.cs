using d360.core.enums;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AllocationDataModelRollupPathSegmentLink
    {
        public int IntersectTypeID { get; set; }
        public PredicateType PredicateType { get; set; }
        public int StartPosition { get; set; }
        public int StartAssetTypeID { get; set; }
        public AssetTypeClass StartClass { get; set; }
        public int EndPosition { get; set; }
        public int EndAssetTypeID { get; set; }
        public AssetTypeClass EndClass { get; set; }
    }
}
