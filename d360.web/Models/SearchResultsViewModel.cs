using d360.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class SearchResultsViewModel
    {
        public SearchResultsViewModel()
        {
            Results = new List<IndexResult>();
            Categories = new List<IndexCategory>();
            ElapsedTime = string.Empty;
        }

        public List<IndexResult> Results { get; set; }
        public List<IndexCategory> Categories { get; set; }
        public string ElapsedTime { get; set; }
    }
}