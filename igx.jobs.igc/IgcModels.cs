using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;

namespace igx.jobs.igc
{
    #region IGC

    #region Type Classes

    public class IgcTypeModel
    {
        [JsonProperty(PropertyName = "_id")]
        public string TypeName { get; set; }

        [JsonProperty(PropertyName = "_name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "editInfo")]
        public IgcTypeEditInfoModel EditInfo { get; set; }
    }

    public class IgcTypeEditInfoModel
    {
        [JsonProperty(PropertyName = "properties")]
        public List<IgcTypeEditInfoPropertyModel> Properties { get; set; }
    }

    public class IgcTypeEditInfoPropertyModel
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }


        [JsonProperty(PropertyName = "type")]
        public IgcTypeEditInfoPropertyTypeModel Type { get; set; }
    }

    public class IgcTypeEditInfoPropertyTypeModel
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "validValues")]
        public List<IgcTypeEditInfoPropertyTypeEnumValueModel> Values { get; set; }
        //[JsonProperty(PropertyName = "type")]
        //public IgcTypeEditInfoPropertyTypeEnumModel Type { get; set; }
    }

    public class IgcTypeEditInfoPropertyTypeEnumModel
    {
        [JsonProperty(PropertyName = "validValues")]
        public List<IgcTypeEditInfoPropertyTypeEnumValueModel> Values { get; set; }
    }

    public class IgcTypeEditInfoPropertyTypeEnumValueModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }
    }

    #endregion

    public class EnumResolutionModel
    {
        public string PropertyName { get; set; }
        public string Code { get; set; }

        public string DisplayValue{ get; set; }
    }

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

    public class RelationshipAction
    {
        //IntersectTypeID, IntersectID, [Action]
        public int IntersectTypeID { get; set; }
        public int IntersectID { get; set; }
        public string Action { get; set; }
    }

    public class ExecutionAssetType
    {
        public long ExecutionID { get; set; }
        public int SynchedAssetTypeID { get; set; }
    }

    public enum PageDataClass
    {
        Fields,
        Relations,
        Responsibilities
    }

    public class PageBeginValueUpdatedEventArgs : EventArgs
    {
        public int Value { get; set; }
        public PageDataClass Class { get; set; }
    }

    public class PageErrorCapturedEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class IgcPageErrorModel
    {
        public string message { get; set; }
        public HttpStatusCode code { get; set; }
    }
}
