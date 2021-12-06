using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    #region Helper Classes

    public class SemanticHeaderFilterValue
    {
        public string @operator { get; set; }
        public string value { get; set; }
    }

    public class SemanticHeaderFilter
    {
        public string match { get; set; }
        public List<SemanticHeaderFilterValue> values { get; set; }
    }

    public class SemanticUserModel
    {
        [JsonProperty("uid")] 
        public Guid Uid { get; set; }
        
        [JsonProperty("fullName")]
        public string FullName { get; set; }
    }

    #endregion

    public abstract class SemanticBase
    {
        [JsonProperty("baseType")]
        public SemanticBaseType BaseType { get; set; }

        [JsonProperty("description"), Column(TypeName = "nvarchar")]
        public string Description { get; set; }

        [JsonIgnore, Column("HeaderFilter", TypeName = "nvarchar")]
        protected string _HeaderFilter { get; private set; }

        [JsonProperty("headerRegExps"), NotMapped]
        public SemanticHeaderFilter HeaderFilter
        {
            get { return JsonConvert.DeserializeObject<SemanticHeaderFilter>(_HeaderFilter ?? "{ values: [] }"); }
            set { _HeaderFilter = JsonConvert.SerializeObject(value); }
        }

        [JsonProperty("headerRegExpConfidence")]
        public int? HeaderFilterConfidence { get; set; }

        [JsonIgnore, Column("InvalidValues", TypeName = "nvarchar")]
        protected string _InvalidValues { get; private set; }

        [JsonProperty("invalidList"), NotMapped]
        public List<string> InvalidValues
        {
            get { return JsonConvert.DeserializeObject<List<string>>(_InvalidValues ?? "[]"); }
            set { _InvalidValues = JsonConvert.SerializeObject(value); }
        }

        [JsonIgnore, Column("JsonPayload", TypeName = "nvarchar")]
        protected string _JsonPayload { get; private set; }

        [JsonProperty("advanced"), NotMapped]
        public JObject JsonPayload
        {
            get { return JObject.Parse(_JsonPayload); }
            set { _JsonPayload = JsonConvert.SerializeObject(value); }
        }

        [JsonProperty("matchType")]
        public SemanticMatchType MatchType { get; set; }

        [JsonProperty("maximum")]
        public float? Maximum { get; set; }

        [JsonProperty("minimum")]
        public float? Minimum { get; set; }

        [JsonProperty("minSamples")]
        public int? MinimumSamples { get; set; }

        [JsonProperty("minMaxPresent")]
        public bool? MinMaxPresent { get; set; }

        [JsonProperty("name"), Column(TypeName = "nvarchar"), StringLength(250)]
        public string Name { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("qualifier"), Column(TypeName = "nvarchar")]
        public string Qualifier { get; set; }

        [JsonProperty("regExReturned"), Column(TypeName = "nvarchar")]
        public string RegularExpression { get; set; }

        [JsonProperty("status")]
        public SemanticStatus Status { get; set; }

        [JsonProperty("threshold")]
        public int Threshold { get; set; }

        [JsonIgnore, Column("ValidLocales", TypeName = "nvarchar")]
        protected string _ValidLocales { get; private set; }

        [JsonProperty("validLocales"), NotMapped]
        public List<string> ValidLocales
        {
            get { return JsonConvert.DeserializeObject<List<string>>(_ValidLocales ?? "[]"); }
            set { _ValidLocales = JsonConvert.SerializeObject(value); }
        }

        [JsonIgnore, Column("ValidValues", TypeName = "nvarchar")]
        protected string _ValidValues { get; private set; }

        [JsonProperty("validList"), NotMapped]
        public List<string> ValidValues
        {
            get { return JsonConvert.DeserializeObject<List<string>>(_ValidValues ?? "[]"); }
            set { _ValidValues = JsonConvert.SerializeObject(value); }
        }
    }

    public class GetSemantics 
    {
        public int total { get; set; }
        public int pageNum { get; set; }
        public int pageSize { get; set; }
        public List<GetSemantic> items { get; set; }
    }

    public class GetSemantic : SemanticBase
    {
        [JsonProperty("createdBy")]
        public SemanticUserModel CreatedBy { get; set; }

        [JsonProperty("createdOn")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("effectiveDate")]
        public DateTime EffectiveDate { get; set; }

        [JsonProperty("source"), DataMember]
        public SemanticSource Source { get; set; }

        [JsonProperty("updatedBy")]
        public SemanticUserModel UpdatedBy { get; set; }

        [JsonProperty("updatedOn")]
        public DateTime UpdatedOn { get; set; }
    }

    public class PatchSemantic : SemanticBase
    {

    }

    public class PostSemantic : SemanticBase
    { 
    
    }

    public class PutSemantic : SemanticBase
    {

    }

    public class Semantic: SemanticBase
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime EffectiveDate { get; set; }

        public SemanticSource Source { get; set; }

        public int UpdatedBy { get; set; }

        public DateTime UpdatedOn { get; set; }
    }

    #region Extensions

    public static class SemanticExtensions
    {
        public static GetSemantic ToGetModel(this Semantic model)
        {
            return new GetSemantic
            {
                BaseType = model.BaseType,
                CreatedBy = new SemanticUserModel
                {
                    FullName = "",
                    Uid = Guid.Empty
                },
                CreatedOn = model.CreatedOn,
                Description = model.Description,
                EffectiveDate = model.EffectiveDate,
                HeaderFilter = model.HeaderFilter,
                HeaderFilterConfidence = model.HeaderFilterConfidence,
                InvalidValues = model.InvalidValues,
                JsonPayload = model.JsonPayload,
                MatchType = model.MatchType,
                Maximum = model.Maximum,
                Minimum = model.Minimum,
                MinimumSamples = model.MinimumSamples,
                MinMaxPresent = model.MinMaxPresent,
                Name = model.Name,
                Priority = model.Priority,
                Qualifier = model.Qualifier,
                RegularExpression = model.RegularExpression,
                Source = model.Source,
                Status = model.Status,
                Threshold = model.Threshold,
                UpdatedBy = new SemanticUserModel
                {
                    FullName = "",
                    Uid = Guid.Empty
                },
                UpdatedOn = model.UpdatedOn,
                ValidLocales = model.ValidLocales,
                ValidValues = model.ValidValues
            };
        }
    }

    #endregion
}
