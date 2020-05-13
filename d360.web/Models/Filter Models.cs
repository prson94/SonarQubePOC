using d360.extensions;
using System.Collections.Generic;

namespace d360.web.Models
{
    public class SearchResultsViewModel
    {
        public SearchResultsViewModel()
        {
            Result = new IndexResults();
            Categories = new List<IndexTypeList>();            
        }

        public IndexResults Result { get; set; }
        public List<IndexTypeList> Categories { get; set; }        
    }
}