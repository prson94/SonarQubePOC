using System.Collections.Generic;
using Newtonsoft.Json;

namespace d360.core.search
{
	public class QueryRequest
    {
        public const int SEARCH_TERM_MAX_LENGTH = 255;
        
        public QueryRequest()
        {
            AggregationFilters = new List<AggregationFilter>();
            FieldFilters = new List<FieldFilter>();
            Aggregations = new List<string>();
            FieldBoosters = new List<FieldBoost>();
        }
        
        private string _term;
        public string Term
        {
            get
            {
                return _term;
            }
            set
            {
                if (value != null && value.Length > SEARCH_TERM_MAX_LENGTH)
                {
                    _term = value.Substring(0, SEARCH_TERM_MAX_LENGTH);
                }
                else
                {
                    _term = value;
                }
            }
        }
        public int Size { get; set; } = 100;
        
        public int From { get; set; } = 0;
        
        public List<AggregationFilter> AggregationFilters { get; set; }
        
        public List<FieldFilter> FieldFilters { get; set; }
        
        public List<string> Aggregations { get; set; }
        
        public SearchConnector SearchConnector { get; set; } = SearchConnector.And;
        
        public bool Explain { get; set; } = false;
        
        [JsonIgnore]
        public List<FieldBoost> FieldBoosters { get; set; }
    }
}
