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
        public MetricRuleResultOperation ResultOperation { get; set; }
        public Guid ResultPathUid { get; set; }
        public MetricMatchType FilterMatchType { get; set; }
        public List<MetricAssetDefinitionDataQualityFilterViewModel> Filters { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionDataQualityFilterViewModel
    {
        public Guid AssetTypeUid { get; set; }
        public string FieldTypeName { get; set; }
        public Operator Operator { get; set; }
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceViewModel
    {
        public MetricGovernanceCheckType Check { get; set; }

        public MetricAssetDefinitionGovernanceFieldViewModel Field { get; set; }
        public MetricAssetDefinitionGovernancePredicateViewModel Predicate { get; set; }
        public MetricAssetDefinitionGovernanceRelationViewModel Relation { get; set; }
        public MetricAssetDefinitionGovernanceOwnerViewModel Owner { get; set; }
        public MetricAssetDefinitionGovernanceExternalViewModel External { get; set; }
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
        public Guid AssetTypeUid { get; set; }
        public string FieldTypeName { get; set; }
        public Operator Operator { get; set; }
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernancePredicateViewModel
    {
        public Guid PredicateUid { get; set; }
        public Operator Operator { get; set; }
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceRelationViewModel
    {
        public Guid IntersectTypeUid { get; set; }
        public Operator Operator { get; set; }
        public List<string> Values { get; set; }
    }

    [DataContract]
    public class MetricAssetDefinitionGovernanceOwnerViewModel
    {
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

        [DataMember, StringLength(10)]
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
