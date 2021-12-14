using d360.core.enums;
using d360.core.exceptions;
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

    public abstract class SemanticBase : BaseObject
    {
        [JsonProperty("baseType")]
        public SemanticBaseType BaseType { get; set; }

        [JsonProperty("description"), Column(TypeName = "nvarchar")]
        public string Description { get; set; }

        [JsonProperty("headerRegExps"), NotMapped]
        public SemanticHeaderFilter HeaderFilter { get; set; }

        [JsonProperty("headerRegExpConfidence")]
        public int? HeaderFilterConfidence { get; set; }

        [JsonProperty("invalidList"), NotMapped]
        public List<string> InvalidValues { get; set; }



        [JsonProperty("advanced"), NotMapped]
        public JObject JsonPayload { get; set; }

        [JsonProperty("matchType")]
        public SemanticMatchType MatchType { get; set; }

        [JsonProperty("maximum")]
        public decimal? Maximum { get; set; }

        [JsonProperty("minimum")]
        public decimal? Minimum { get; set; }

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

        [JsonProperty("validLocales"), NotMapped]
        public List<string> ValidLocales { get; set; }
 
        [JsonProperty("validList"), NotMapped]
        public List<string> ValidValues { get; set; }
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
        [JsonProperty("baseType")]
        public new SemanticBaseType? BaseType { get; set; }

        [JsonProperty("matchType")]
        public new SemanticMatchType? MatchType { get; set; }

        [JsonProperty("priority")]
        public new int? Priority { get; set; }

        [JsonProperty("status")]
        public new SemanticStatus? Status { get; set; }

        [JsonProperty("threshold")]
        public new int? Threshold { get; set; }
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

        [StringLength(10)]
        public string TransactionId { get; set; }


        #region Internal Fields To Manage Complex Objects

        [Column("HeaderFilter", TypeName = "nvarchar")]
        internal string _HeaderFilter { get; set; }

        [Column("InvalidValues", TypeName = "nvarchar")]
        internal string _InvalidValues { get; set; }
        
        [Column("JsonPayload", TypeName = "nvarchar")]
        internal string _JsonPayload { get; set; }
        
        [Column("ValidLocales", TypeName = "nvarchar")]
        internal string _ValidLocales { get; set; }
        
        [Column("ValidValues", TypeName = "nvarchar")]
        internal string _ValidValues { get; set; }

        #endregion

        internal T deserializeTextProperty<T>(string propertyValue)
        {
            return string.IsNullOrEmpty(propertyValue) ? default(T) : JsonConvert.DeserializeObject<T>(propertyValue);
        }

        internal string serializeTextProperty<T>(T propertyValue)
        {
            return (propertyValue == null) ? null : JsonConvert.SerializeObject(propertyValue);
        }
    }

    #region Extensions

    public static class SemanticExtensions
    {
        public static GetSemantic ToGetModel(this Semantic model, GlobalReportingResource createdBy, GlobalReportingResource updatedBy)
        {
            return new GetSemantic
            {
                BaseType = model.BaseType,
                CreatedBy = new SemanticUserModel
                {
                    FullName = createdBy.FullName,
                    Uid = createdBy.Uid
                },
                CreatedOn = model.CreatedOn,
                Description = model.Description,
                EffectiveDate = model.EffectiveDate,
                HeaderFilter = model.deserializeTextProperty<SemanticHeaderFilter>(model._HeaderFilter),
                HeaderFilterConfidence = model.HeaderFilterConfidence,
                InvalidValues = model.deserializeTextProperty<List<string>>(model._InvalidValues),
                JsonPayload = (string.IsNullOrEmpty(model._JsonPayload)) ? null : JObject.Parse(model._JsonPayload),
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
                    FullName = updatedBy.FullName,
                    Uid = updatedBy.Uid
                },
                UpdatedOn = model.UpdatedOn,
                ValidLocales = model.deserializeTextProperty<List<string>>(model._ValidLocales),
                ValidValues = model.deserializeTextProperty<List<string>>(model._ValidValues)
            };
        }

        public static Semantic ToRepositoryModel(this PostSemantic model, int resourceId)
        {
            var date = DateTime.UtcNow;

            var repoModel = new Semantic
            {
                BaseType = model.BaseType,
                CreatedBy = resourceId,
                CreatedOn = date,
                Description = model.Description,
                EffectiveDate = date,
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
                Source = SemanticSource.UserDefined,
                Status = model.Status,
                Threshold = model.Threshold,
                UpdatedBy = resourceId,
                UpdatedOn = date,
                ValidLocales = model.ValidLocales,
                ValidValues = model.ValidValues
            };

            repoModel._HeaderFilter = repoModel.serializeTextProperty(repoModel.HeaderFilter);
            repoModel._InvalidValues = repoModel.serializeTextProperty(repoModel.InvalidValues);
            repoModel._JsonPayload = repoModel.serializeTextProperty(repoModel.JsonPayload);
            repoModel._ValidLocales = repoModel.serializeTextProperty(repoModel.ValidLocales);
            repoModel._ValidValues = repoModel.serializeTextProperty(repoModel.ValidValues);

            return repoModel;
        }

        public static Semantic ToRepositoryModel(this PutSemantic model, Semantic existing, int resourceId)
        {
            var date = DateTime.UtcNow;

            var repoModel = new Semantic
            {
                BaseType = model.BaseType,
                CreatedBy = existing.CreatedBy,
                CreatedOn = existing.CreatedOn,
                Description = model.Description,
                EffectiveDate = date,
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
                Source = existing.Source,
                Status = model.Status,
                Threshold = model.Threshold,
                UpdatedBy = resourceId,
                UpdatedOn = date,
                ValidLocales = model.ValidLocales,
                ValidValues = model.ValidValues
            };

            repoModel._HeaderFilter = repoModel.serializeTextProperty(repoModel.HeaderFilter);
            repoModel._InvalidValues = repoModel.serializeTextProperty(repoModel.InvalidValues);
            repoModel._JsonPayload = repoModel.serializeTextProperty(repoModel.JsonPayload);
            repoModel._ValidLocales = repoModel.serializeTextProperty(repoModel.ValidLocales);
            repoModel._ValidValues = repoModel.serializeTextProperty(repoModel.ValidValues);

            return repoModel;
        }

        public static Semantic ToRepositoryModel(this PatchSemantic model, Semantic existing, int resourceId)
        {
            var date = DateTime.UtcNow;

            var repoModel = new Semantic
            {
                BaseType = model.BaseType ?? existing.BaseType,
                CreatedBy = existing.CreatedBy,
                CreatedOn = existing.CreatedOn,
                Description = model.Description ?? existing.Description,
                EffectiveDate = date,
                HeaderFilter = (model.HeaderFilter != null) ? model.HeaderFilter : existing.HeaderFilter,
                HeaderFilterConfidence = model.HeaderFilterConfidence ?? existing.HeaderFilterConfidence,
                InvalidValues = (model.InvalidValues != null) ? model.InvalidValues : existing.InvalidValues,
                JsonPayload = model.JsonPayload ?? existing.JsonPayload,
                MatchType = model.MatchType ?? existing.MatchType,
                Maximum = model.Maximum ?? existing.Maximum,
                Minimum = model.Minimum ?? existing.Minimum,
                MinimumSamples = model.MinimumSamples ?? existing.MinimumSamples,
                MinMaxPresent = model.MinMaxPresent ?? existing.MinMaxPresent,
                Name = model.Name ?? existing.Name,
                Priority = model.Priority ?? existing.Priority,
                Qualifier = model.Qualifier,
                RegularExpression = model.RegularExpression ?? existing.RegularExpression,
                Source = existing.Source,
                Status = model.Status ?? existing.Status,
                Threshold = model.Threshold ?? existing.Threshold,
                UpdatedBy = resourceId,
                UpdatedOn = date,
                ValidLocales = (model.ValidLocales != null) ? model.ValidLocales : existing.ValidLocales,
                ValidValues = (model.ValidValues != null) ? model.ValidValues : existing.ValidValues
            };

            repoModel._HeaderFilter = repoModel.serializeTextProperty(repoModel.HeaderFilter);
            repoModel._InvalidValues = repoModel.serializeTextProperty(repoModel.InvalidValues);
            repoModel._JsonPayload = repoModel.serializeTextProperty(repoModel.JsonPayload);
            repoModel._ValidLocales = repoModel.serializeTextProperty(repoModel.ValidLocales);
            repoModel._ValidValues = repoModel.serializeTextProperty(repoModel.ValidValues);

            return repoModel;
        }

        public static void Validate(this Semantic model)
        {
            var errors = new List<string>();

            #region Common validation funcs

            Func<bool> headerFilterPopulated = () => 
            {
                if (model.HeaderFilter != null)
                {
                    if (model.HeaderFilter.values != null)
                    {
                        return (model.HeaderFilter.values.Count > 0) || !string.IsNullOrEmpty(model.HeaderFilter.match);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            };

            Func<bool> jsonPayloadPopulated = () =>
            {
                return (!string.IsNullOrEmpty(model._JsonPayload));
            };

            #endregion

            if (headerFilterPopulated())
            {
                if (!model.HeaderFilterConfidence.HasValue)
                {
                    errors.Add("HeaderFilterConfidence must be populated if HeaderFilter is.");
                }
            }

            if (model.HeaderFilterConfidence.HasValue && (model.HeaderFilterConfidence.Value < 1 || model.HeaderFilterConfidence.Value > 100))
            {
                errors.Add("HeaderFilterConfidence must be a whole number value between 1 and 100.");
            }

            if (!string.IsNullOrEmpty(model._JsonPayload))
            {
                try
                {
                    JToken.Parse(model._JsonPayload);
                }
                catch
                {
                    errors.Add("While JsonPayload is populated, it is not currently valid.");
                }
            }

            decimal minValue = -999999999999.999999m;
            decimal maxValue = 999999999999.999999m;

            if (model.BaseType == SemanticBaseType.Long && model.Minimum.HasValue)
            {
                if (!long.TryParse(model.Minimum.Value.ToString(), out _))
                {
                    errors.Add("Since BaseType is Long, Minimum must be a whole number value.");
                }
            }
            else if (model.BaseType != SemanticBaseType.Long && model.BaseType != SemanticBaseType.Double && model.Minimum.HasValue)
            {
                errors.Add("Since BaseType is not Long or Double, Minimum must not contain a value.");
            }

            if (model.BaseType == SemanticBaseType.Long && model.Maximum.HasValue)
            {
                if (!long.TryParse(model.Maximum.Value.ToString(), out _))
                {
                    errors.Add("Since BaseType is Long, Maximum must be a whole number value.");
                }
            }
            else if (model.BaseType != SemanticBaseType.Long && model.BaseType != SemanticBaseType.Double && model.Maximum.HasValue)
            {
                errors.Add("Since BaseType is not Long or Double, Maximum must not contain a value.");
            }

            if (model.Minimum.HasValue && model.Maximum.HasValue && model.Minimum.Value > model.Maximum.Value)
            {
                errors.Add("Minimum must not be greater than Maximum.");
            }
            if (model.Minimum.HasValue)
            {
                if (model.Minimum.Value < minValue || model.Minimum.Value > maxValue)
                {
                    errors.Add($"Minimum must not fall outside the range of {minValue} or {maxValue}.");
                }
            }
            if (model.Maximum.HasValue)
            {
                if (model.Maximum.Value < minValue || model.Maximum.Value > maxValue)
                {
                    errors.Add($"Maximum must not fall outside the range of {minValue} or {maxValue}.");
                }
            }

            if ((!model.Minimum.HasValue || !model.Maximum.HasValue) && model.MinMaxPresent.HasValue)
            {
                errors.Add("Both Minimum AND Maximum must contain values for MinMaxPresent to be used. Otherwise it must be removed.");
            }

            if (model.Priority < 1)
            {
                errors.Add("Priority must contain a value of 1 or greater.");
            }

            if (model.Threshold < 0 || model.Threshold > 100)
            {
                errors.Add("Threshold must contain a whole number value between 0 and 100.");
            }

            #region Advanced Validation

            switch(model.MatchType)
            {
                case SemanticMatchType.Advanced:
                    if (headerFilterPopulated())
                    {
                        errors.Add("Since MatchType is Advanced, HeaderFilter must be empty.");
                    }

                    if (model.InvalidValues != null && model.InvalidValues.Count > 0)
                    {
                        errors.Add("Since MatchType is Advanced, InvalidValues must be empty.");
                    }

                    if (!jsonPayloadPopulated())
                    {
                        errors.Add("Since MatchType is Advanced, JsonPayload must not be empty.");
                    }

                    if (model.MinimumSamples.HasValue)
                    {
                        errors.Add("Since MatchType is Advanced, MinimumSamples must be empty.");
                    }

                    if (!string.IsNullOrEmpty(model.RegularExpression))
                    {
                        errors.Add("Since MatchType is Advanced, RegularExpression must be empty.");
                    }

                    if (model.ValidValues != null && model.ValidValues.Count > 0)
                    {
                        errors.Add("Since MatchType is Advanced, ValidValues must be empty.");
                    }
                    break;
                case SemanticMatchType.List:
                    if (jsonPayloadPopulated())
                    {
                        errors.Add("Since MatchType is List, JsonPayload must be empty.");
                    }
                    if (!string.IsNullOrEmpty(model.RegularExpression))
                    {
                        errors.Add("Since MatchType is List, RegularExpression must be empty.");
                    }
                    break;
                case SemanticMatchType.Number:
                    if (!headerFilterPopulated())
                    {
                        errors.Add("Since MatchType is Number, HeaderFilter must not be empty.");
                    }
                    if (jsonPayloadPopulated())
                    {
                        errors.Add("Since MatchType is Number, JsonPayload must be empty.");
                    }
                    break;
                case SemanticMatchType.Pattern:
                    if (jsonPayloadPopulated())
                    {
                        errors.Add("Since MatchType is Pattern, JsonPayload must be empty.");
                    }
                    if (string.IsNullOrEmpty(model.RegularExpression))
                    {
                        errors.Add("Since MatchType is Pattern, RegularExpression must not be empty.");
                    }
                    break;
                default:
                    // Do nothing.
                    break;
            }

            #endregion Advanced Validation


            // Determine if we should throw an error.
            if (errors.Count > 0)
            {
                throw new GenericException(System.Net.HttpStatusCode.BadRequest, "This semantic is invalid.", string.Join("; ", errors));
            }
        }
    }

    #endregion
}
