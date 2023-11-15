using System.Collections.Generic;

namespace d360.core.search
{

	public class QueryLimitation
    {
        public QueryLimitation()
        {
            AggregationFilters = new List<AggregationFilter>();
            ResourceGroupIDs = new List<int>();
        }
        
        public List<AggregationFilter> AggregationFilters { get; set; }
        
        public bool HideData3SixtyUsers { get; set; } = false;
        
        public int ResourceID { get; set; }
        
        public List<int> ResourceGroupIDs { get; set; }
        
		public bool IsAdministrator { get; set; }
    }
}
