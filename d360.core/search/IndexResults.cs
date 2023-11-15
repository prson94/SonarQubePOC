using System.Collections.Generic;

namespace d360.core.search
{
	public class IndexResults
    {
        public IndexResults()
        {
            Results = new List<IndexResult>();
            Aggregations = new Dictionary<string, List<IndexAggregation>>();
            ElapsedMS = new Dictionary<string, int>();
        }

        public List<IndexResult> Results { get; set; }
        public Dictionary<string, List<IndexAggregation>> Aggregations { get; set; }
        public int Matches { get; set; }
        public Dictionary<string, int> ElapsedMS { get; set; }
    }
}
