using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace igx.jobs.igc
{
    #region IGC

    public class GenericIgcContextModel
    {
        public string _type { get; set; }
        public string _id { get; set; }
        public string _url { get; set; }
        public string _name { get; set; }
    }

    public class GenericIgcPagingModel
    {
        public int numTotal { get; set; }
        public string next { get; set; }
        public int pageSize { get; set; }
        public int end { get; set; }
        public int begin { get; set; }
    }

    public class IgcModel
    {
        [JsonProperty(PropertyName = "_id")]
        public string SourceID { get; set; }

        [JsonProperty(PropertyName = "_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty(PropertyName = "_url")]
        public string IgcUrl { get; set; }

        [JsonProperty(PropertyName = "_type")]
        public string Type { get; set; }

        public List<GenericIgcContextModel> _context { get; set; }
    }

    public class IgcModels
    {
        public GenericIgcPagingModel paging { get; set; }
    }

    public class IgcDynamicArrayModels : IgcModels
    {
        public JArray items { get; set; }
    }

    public class IgcRelationshipModel : IgcModels
    {
        public List<IgcModel> items { get; set; }
    }

    public class IgcPostSearchRequestModel
    {
        public int? begin { get; set; }
        public int? pageSize { get; set; }
        public List<string> types { get; set; } = new List<string>();
        public IgcPostSearchRequestWhereModel where { get; set; } = new IgcPostSearchRequestWhereModel();
        public List<IgcPostSearchRequestSortModel> sorts { get; set; }
        public List<string> properties { get; set; } = new List<string>();
    }

    public class IgcPostSearchRequestSortModel
    {
        public string property { get; set; }
        public bool ascending { get; set; }
    }

    public class IgcPostSearchRequestWhereModel
    {
        public List<IIgcPostSearchRequestWhereConditionModel> conditions { get; set; } = new List<IIgcPostSearchRequestWhereConditionModel>();
        public string @operator { get; set; } = "or";
    }

    public interface IIgcPostSearchRequestWhereConditionModel
    {
        string property { get; set; }
    }

    public class IgcPostSearchRequestBetweenConditionModel: IIgcPostSearchRequestWhereConditionModel
    {
        public long? min { get; set; }
        public long? max { get; set; }
        public string property { get; set; }
        public string @operator { get; } = "between";
    }

    public class IgcPostSearchRequestEqualConditionModel : IIgcPostSearchRequestWhereConditionModel
    {
        public string value { get; set; }
        public string property { get; set; }
        public string @operator { get; } = "=";
    }

    public class IgcPostSearchRequestContainsConditionModel : IIgcPostSearchRequestWhereConditionModel
    {
        public string value { get; set; }
        public string property { get; set; }
        public string @operator { get; } = "like %{0}%";
    }

    public class IgcPostSearchRequestStartsWithConditionModel : IIgcPostSearchRequestWhereConditionModel
    {
        public string value { get; set; }
        public string property { get; set; }
        public string @operator { get; } = "like {0}%";
    }

    public class IgcPostSearchRequestEndsWithConditionModel : IIgcPostSearchRequestWhereConditionModel
    {
        public string value { get; set; }
        public string property { get; set; }
        public string @operator { get; } = "like %{0}";
    }

    #endregion

    public class D3sOwnershipItemsModel
    {
        public string UserIdFieldName { get; set; }
        public List<D3sOwnershipModel> Items { get; set; }
    }

    public class D3sOwnershipModel
    {
        public string SourceID { get; set; }
        public string RoleName { get; set; }
        public string UserId { get; set; }

        [JsonIgnore]
        public string UserFullName { get; set; }
    }

    public class D3sRelationshipModel
    {
        public string SubjectSourceID { get; set; }

        public string ObjectSourceID { get; set; }

        public int PredicateType { get; set; }

        public int IntersectTypeID { get; set; }
    }
}
