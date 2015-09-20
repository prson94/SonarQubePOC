using d360.extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class ArtifactListFilterModel
    {
        public ArtifactListFilterModel()
        {
            IDs = new List<int>();
        }
        public string RelationFilterType { get; set; }
        public string Type { get; set; }
        public List<int> IDs { get; set; }
        
        //public string Name { get; set; }
        //public string Description { get; set; }

        //public string Statuses { get; set; }
        //public string InformationModels { get; set; }

        //public List<int> GetInformationModelsAsList()
        //{
        //    if (!string.IsNullOrEmpty(InformationModels))
        //    {
        //        var arr = JArray.Parse(InformationModels);
        //        return arr.Values().Select(i => i.ToObject<int>()).ToList();
        //    }
        //    else
        //    {
        //        return new List<int>();
        //    }
        //}

        //public List<string> GetStatusesAsList()
        //{
        //    if (!string.IsNullOrEmpty(Statuses))
        //    {
        //        var arr = JArray.Parse(Statuses);
        //        return arr.Values().Select(i => i.ToString()).ToList();
        //    }
        //    else
        //    {
        //        return new List<string>();
        //    }
        //}
    }

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
            Results = new List<IndexResult>();
            Categories = new List<IndexCategory>();
            ElapsedTime = string.Empty;
        }

        public List<IndexResult> Results { get; set; }
        public List<IndexCategory> Categories { get; set; }
        public string ElapsedTime { get; set; }
    }
}