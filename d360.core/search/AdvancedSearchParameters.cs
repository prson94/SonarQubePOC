namespace d360.core.search
{
	public class AdvancedSearchParameters
    {
        public string field { get; set; }
        
        public string value { get; set; }
        
        public bool exact { get; set; }
        
        public SearchConnector connector { get; set; }
    }
}
