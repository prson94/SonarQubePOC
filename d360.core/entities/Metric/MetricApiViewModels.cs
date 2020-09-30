using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    [DataContract]
    public class MetricAssetViewModel
    {
        [DataMember, JsonProperty(Order = 1)]
        public Guid Uid { get; set; }

        [DataMember, JsonProperty(Order = 2)]
        public Guid? ParentUid { get; set; }

        [DataMember, JsonProperty(Order = 3)]
        public Guid AllocationUid { get; set; }

        [DataMember, JsonProperty(Order = 4)]
        public MetricAssetDefinitionViewModel Definition { get; set; }

        [DataMember, JsonProperty(Order = 6)]
        public bool IsGroup { get; set; }

        [DataMember, JsonProperty(Order = 7)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "You have provided an invalid name.")]
        [MaxLength(250, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string Name { get; set; }

        [DataMember, JsonProperty(Order = 8)]
        public string Description { get; set; }

        [DataMember, JsonProperty(Order = 9)]
        public DateTime EffectiveDate { get; set; }

        [DataMember, JsonProperty(Order = 10)]
        public decimal Weight { get; set; }

        [DataMember, JsonProperty(Order = 11)]
        public double? Threshold { get; set; }

        [DataMember, JsonProperty(Order = 13)]
        public bool MatchConditionsOnly { get; set; } = false;

        [DataMember, JsonProperty(Order = 20)]
        public List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; } = new List<MetricAssetVersionConditionViewModel>();

        [DataMember, JsonProperty(Order = 21)]
        public int VersionCount { get; set; }

        [DataMember, JsonProperty(Order = 22)]
        public bool HasResults { get; set; } = false;

        [IgnoreDataMember]
        public string CurrentConditionHash 
        { 
            get 
            {
                var hashItems = from g in ConditionGroups
                                from c in g.ConditionItems
                                from v in c.Values
                                orderby g.Position, c.ConditionFieldTypeID, c.ConditionIntersectTypeID, v
                                select $"{g.MatchType};{g.Position};{g.Weight};{c.ConditionFieldTypeID};{c.ConditionIntersectTypeID};{c.ConditionType};{c.Operator};{v}";
                string newConditionHash = string.Join("|", hashItems);
                newConditionHash = newConditionHash.GetD3sHashString();
                return newConditionHash;
            }
        }

        /// <summary>
        /// Property below used solely for processing incoming data.
        /// </summary>
        public MetricAllocation Allocation { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionViewModel
    {
        [DataMember]
        public MetricAssetDefinitionDataQualityViewModel DataQuality { get; set; }

        [DataMember] 
        public MetricAssetDefinitionGovernanceViewModel Governance { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionDataQualityViewModel
    {
        [DataMember] 
        public MetricRuleResultOperation ResultOperation { get; set; }
        [DataMember] 
        public Guid ResultPathUid { get; set; }
        [DataMember] 
        public MetricMatchType FilterMatchType { get; set; }
        [DataMember] 
        public List<MetricAssetDefinitionDataQualityFilterViewModel> Filters { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionDataQualityFilterViewModel
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember] 
        public string FieldTypeName { get; set; }
        [DataMember] 
        public Operator Operator { get; set; }
        [DataMember] 
        public List<string> Values { get; set; }

        /// <summary>
        /// Property below used solely for processing incoming data.
        /// </summary>
        public int AssetTypeID { get; set; }
        /// <summary>
        /// Property below used solely for processing incoming data.
        /// </summary>
        public int FieldTypeID { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceViewModel
    {
        [DataMember]
        public MetricGovernanceCheckType Check { get; set; }

        [DataMember]
        public MetricAssetDefinitionGovernanceFieldViewModel Field { get; set; }
        [DataMember] 
        public MetricAssetDefinitionGovernancePredicateViewModel Predicate { get; set; }
        [DataMember] 
        public MetricAssetDefinitionGovernanceRelationViewModel Relation { get; set; }
        [DataMember] 
        public MetricAssetDefinitionGovernanceOwnerViewModel Owner { get; set; }
        [DataMember] 
        public MetricAssetDefinitionGovernanceExternalViewModel External { get; set; }

        public string ValidateCheckObjectCorrespondsToCheck()
        {
            string errorMessage = null;
            string standardMissingObjectError = $"Because you selected {Check} as the type of check, you must provide a {Check} object property under Definition. ";
            string otherObjectPropertiesPopulatedError = $"Because you selected {Check} as the type of check, you may not populate any other object properties under Definition.";
            switch (Check)
            {
                case MetricGovernanceCheckType.External:
                    if (External == null)
                    {
                        errorMessage = standardMissingObjectError;
                    }
                    if (Field != null || Owner != null || Predicate != null || Relation != null)
                    {
                        errorMessage += otherObjectPropertiesPopulatedError;
                    }
                    break;
                case MetricGovernanceCheckType.Field:
                    if (Field == null)
                    {
                        errorMessage = standardMissingObjectError;
                    }
                    if (External != null || Owner != null || Predicate != null || Relation != null)
                    {
                        errorMessage += otherObjectPropertiesPopulatedError;
                    }
                    break;
                case MetricGovernanceCheckType.Owner:
                    if (Owner == null)
                    {
                        errorMessage = standardMissingObjectError;
                    }
                    if (Field != null || External != null || Predicate != null || Relation != null)
                    {
                        errorMessage += otherObjectPropertiesPopulatedError;
                    }
                    break;
                case MetricGovernanceCheckType.Predicate:
                    if (Predicate == null)
                    {
                        errorMessage = standardMissingObjectError;
                    }
                    if (Field != null || Owner != null || External != null || Relation != null)
                    {
                        errorMessage += otherObjectPropertiesPopulatedError;
                    }
                    break;
                case MetricGovernanceCheckType.Relation:
                    if (Relation == null)
                    {
                        errorMessage = standardMissingObjectError;
                    }
                    if (Field != null || Owner != null || Predicate != null || External != null)
                    {
                        errorMessage += otherObjectPropertiesPopulatedError;
                    }
                    break;
            }

            return errorMessage;
        }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceExternalViewModel
    {
        [DataMember]
        public MetricUpdateFrequency UpdateFrequency { get; set; } = MetricUpdateFrequency.None;
        
        [DataMember]
        public string Instructions { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceFieldViewModel
    {
        [DataMember] 
        public string FieldTypeName { get; set; }
        
        [DataMember] 
        public Operator Operator { get; set; }
        
        [DataMember] 
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernancePredicateViewModel
    {
        [DataMember] 
        public Guid PredicateUid { get; set; }
        [DataMember] 
        public Operator Operator { get; set; }
        //[DataMember] 
        //public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceRelationViewModel
    {
        [DataMember] 
        public Guid IntersectTypeUid { get; set; }
        [DataMember] 
        public Operator Operator { get; set; }
        [DataMember] 
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceOwnerViewModel
    {
        [DataMember] 
        public Guid ResponsibilityTypeUid { get; set; }
    }

    [DataContract]
    public class MetricAssetViewDetailModel : MetricAssetViewModel
    {
        [DataMember, JsonProperty(Order = 100)]
        public List<MetricAssetVersionViewModel> Versions { get; set; }
    }

    [DataContract]
    public class MetricAssetVersionViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public double? Threshold { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public MetricUpdateFrequency UpdateFrequency { get; set; } = MetricUpdateFrequency.None;

        [DataMember]
        public bool MatchConditionsOnly { get; set; } = false;

        [DataMember, StringLength(1)]
        public string ConditionAndOr { get; set; }

        [DataMember]
        public DateTime? EffectiveEndDate { get; set; }
    }

    [DataContract]
    public class MetricAssetVersionConditionViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public int Position { get; set; } = 1;

        [DataMember]
        public double? Threshold { get; set; }

        [DataMember]
        public decimal? Weight { get; set; }

        [DataMember]
        public MetricMatchType MatchType { get; set; }

        [DataMember]
        public List<MetricAssetVersionConditionItemViewModel> ConditionItems { get; set; }
    }

    [DataContract]
    public class MetricAssetVersionConditionItemViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }
        
        [DataMember]
        public MetricConditionType ConditionType { get; set; }
        
        public int? ConditionFieldTypeID{ get; set; }

        public int? ConditionIntersectTypeID { get; set; }

        [DataMember]
        public string ConditionFieldTypeName { get; set; }

        [DataMember]
        public Guid? ConditionIntersectTypeUid { get; set; }

        [DataMember]
        public Operator Operator { get; set; }

        [DataMember]
        public List<string> Values { get; set; }

    }

    public class MetricFieldTypeViewModel
    {
        public int ID { get; set; }
        public string ApiName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<MetricFieldTypeValueViewModel> Values { get; set; }
    }

    public class MetricFieldTypeValueViewModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }

    [DataContract]
    public class MetricPathOptionViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }
        [DataMember] 
        public State State { get; set; }
        [DataMember] 
        public string Path { get; set; }
        public string SegmentsJson { get; set; }
        [DataMember] 
        public List<MetricPathOptionSegmentViewModel> Segments
        {
            get
            {
                return JsonConvert.DeserializeObject<List<MetricPathOptionSegmentViewModel>>(SegmentsJson ?? "[]");
            }
        }
    }

    public class MetricPathOptionSegmentViewModel
    {
        public Guid AssetTypeUid { get; set; }
        public string Name { get; set; }
    }

    public class MeasureVersionHistoryModel
    {
        public Guid MeasureUid { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }        
        public DateTime EffectiveDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public double Weight { get; set; }
        public List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; } = new List<MetricAssetVersionConditionViewModel>();
        public bool HasResults { get; set; } = false;
    }
}
