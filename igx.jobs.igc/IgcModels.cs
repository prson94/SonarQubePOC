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

    public class IgcModels
    {
        public GenericIgcPagingModel paging { get; set; }
    }

    public class IgcDynamicArrayModels : IgcModels
    {
        public JArray items { get; set; }
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

    public class AssetHashModel
    {
        public int SynchedAssetTypeID { get; set; }
        public int Section { get; set; }
        public int RequestNumber { get; set; }
        public string SourceID { get; set; }
        public string Hash { get; set; }
        public string Action { get; set; } //returned from db to determine what type of action this was for hash
    }

    public class RelationshipAction
    {
        //IntersectTypeID, IntersectID, [Action]
        public int IntersectTypeID { get; set; }
        public int IntersectID { get; set; }
        public string Action { get; set; }
    }

    public enum PageDataClass
    {
        Fields = 1,
        Relations = 2,
        Responsibilities = 3
    }

    public class IgcPageErrorModel
    {
        public string message { get; set; }
        public HttpStatusCode code { get; set; }
    }


    public class IgcRelationshipCollection : IgcModels
    {
        public List<IgcRelationshipCollectionModel> items { get; set; }
    }

    public class IgcRelationshipCollectionModel
    {
        public string _type { get; set; }
        public string _id { get; set; }
    }

    public class RelationshipTargetComparisonModel
    {
        public string SourceField { get; set; }
        public string SourceAssetType { get; set; }
        public int IntersectTypeID { get; set; }
    }

    public class IGCAssetRelationshipBreakdownModels : List<IGCAssetRelationshipBreakdownModel> { }
    //[{"FieldName":"impacts_on","AssetTypeName":"$RRP-RRPLevel3Service","Count":3561}]
    public class IGCAssetRelationshipBreakdownModel
    {
        public string FieldName { get; set; }
        public string AssetTypeName { get; set; }
        public int IntersectTypeID { get; set; }
        public int Count { get; set; }
    }

    public class IGCAssetResponsibilityBreakdownModels : List<IGCAssetResponsibilityBreakdownModel> { }
    //[{"Role":"$EDGMStewardId","Count":1951},{"Role":"custom_Data Quality Administrator ID","Count":13}]
    public class IGCAssetResponsibilityBreakdownModel
    {
        public string Role { get; set; }
        public int Count { get; set; }
    }

    /// <summary>
    /// GOV-5373: Removes trailing .0 on any inferred numbers.
    /// </summary>
    internal class DecimalJsonConverter : JsonConverter
    {
        public DecimalJsonConverter()
        {
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (DecimalJsonConverter.IsWholeValue(value))
            {
                writer.WriteRawValue(JsonConvert.ToString(Convert.ToInt64(value)));
            }
            else
            {
                writer.WriteRawValue(JsonConvert.ToString(value));
            }
        }

        private static bool IsWholeValue(object value)
        {
            if (value is decimal)
            {
                decimal decimalValue = (decimal)value;
                int precision = (Decimal.GetBits(decimalValue)[3] >> 16) & 0x000000FF;
                return precision == 0;
            }
            else if (value is float || value is double)
            {
                double doubleValue = (double)value;
                return doubleValue == Math.Truncate(doubleValue);
            }

            return false;
        }
    }
    
    public class IgcException
    {
        public string ClassName { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }

        public IgcException InnerException { get; set; }

        public string GetErrorMessage()
        {
            var error = $"{this.ClassName}::{this.Message}";

            var inner = this.InnerException;
            while (inner != null)
            {
                error += ", " + inner.Message;
                inner = inner.InnerException;
            }
            return error;
        }
    }

    #region EventArgs

    public class PageBeginValueUpdatedEventArgs : EventArgs
    {
        public int Value { get; set; }
        public PageDataClass Class { get; set; }
    }

    public class PageProcessedInGovernUpdatedEventArgs : EventArgs
    {
        public int Value { get; set; }
        public PageDataClass Class { get; set; }
    }

    public class PageErrorCapturedEventArgs : EventArgs
    {
        public string ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }

    public class RelationshipBreakdownModelsUpdatedEventArgs : EventArgs
    {
        public List<IGCAssetRelationshipBreakdownModel> Updates { get; set; }
    }

    public class ResponsibilityBreakdownModelsUpdatedEventArgs : EventArgs
    {
        public IGCAssetResponsibilityBreakdownModel Update { get; set; }
    }

    public class StepStartedEventArgs : EventArgs
    {
        public int Step { get; set; }
    }

    public class StepCompletedEventArgs : EventArgs
    {
        public int Step { get; set; }
    }

    #endregion
}
