using System.Collections.Generic;

namespace d360.core.search
{
	public class QueryRequest
    {
        public const int SEARCH_TERM_MAX_LENGTH = 255;
        
        public QueryRequest()
        {
            AggregationFilters = new List<AggregationFilter>();
        }
        
        public string Term { get; set; }
        
		public int Size { get; set; } = 100;
        
        public int From { get; set; } = 0;

		public bool IncludeAggregations { get; set; }

		public List<AggregationFilter> AggregationFilters { get; set; }
    }
}
