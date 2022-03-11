using System;

namespace d360.core.entities
{
    /// <summary>
    /// The returned object after a responsibility rule runs and an asset is (un)assigned an owner. Used internally by the responsibility rules engine.
    /// </summary>
    public class ResponsibilityAssetMeasureProcessedResult
    {
        public Guid AssetUid { get; set; }
        
        public Guid AllocationUid { get; set; }
        
        public Guid MetricAssetUid { get; set; }
        
        public Guid MetricAssetVersionUid { get; set; }
    }
}
