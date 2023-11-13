using System;

namespace d360.core.search
{
	public class IndexableStatus : IndexableCount
    {
        public int Status { get; set; }
        
        public int TargetCount { get; set; }

		public int DatabaseCount { get; set; }
        
        public DateTime Start { get; set; }
        
        public DateTime LastUpdate { get; set; }
    }
}
