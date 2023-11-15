using System;

namespace d360.core.search
{
	public class IndexAssetScore
    {
        public Guid AssetUid { get; set; }
        
        public string EffectiveDate { get; set; }
        
        public string EndDate { get; set; }
        
        public string Rundate { get; set; }
        
        public string ScoreType { get; set; }
        
        public decimal Value { get; set; }
        
        public int LowerThreshold { get; set; }
        
        public int UpperThreshold { get; set; }
    }
}
