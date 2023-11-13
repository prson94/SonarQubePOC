namespace d360.core.search
{
	public class FieldFilter
    {
        public string Field { get; set; }
        
        public string[] Values { get; set; }
        
        public SearchConnector Connector { get; set; } = SearchConnector.Or;
        
        public SearchOperator Operator { get; set; } = SearchOperator.Contains;
        
        public bool MatchWords { get; set; } = false;
    }
}
