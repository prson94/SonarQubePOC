using System.Collections.Generic;

namespace d360.core.search
{
	public class IndexAggregation
    {

        public IndexAggregation()
        {
            Items = new List<IndexAggregation>();
        }

        public string Name { get; set; }
        
        public string DisplayName { get; set; }
        
        public int ResultCount { get; set; }

        public List<IndexAggregation> Items { get; set; }
    }
}
