using d360.extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    
    public class GroupSearchResultModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public int NumberOfMembers { get; set; }

        public bool IsMember { get; set; }
    }

    public class PersonSearchResultModel
    {
        public int ID { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

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