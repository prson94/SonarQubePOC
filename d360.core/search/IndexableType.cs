using System;

namespace d360.core.search
{
	public class IndexableType
    {
        public string Name { get; set; }
        
        public int Class { get; set; }
        
        public string ClassName { get; set; }
        
        public Guid AssetTypeUid { get; set; }

		public string AssetTypePath { get; set; }
	}
}
