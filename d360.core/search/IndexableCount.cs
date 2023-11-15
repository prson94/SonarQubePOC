using System;
namespace d360.core.search
{
	public class IndexableCount
    {
        public string ClassName { get; set; }
        
        public int Class { get; set; }
        
        public Guid AssetTypeUid { get; set; }
        
        public int CurrentCount { get; set; }
    }
}
