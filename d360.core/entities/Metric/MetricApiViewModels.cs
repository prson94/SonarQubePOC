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
    public interface IConditionGroupMeasure
    {
        List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; }
    }

    public interface IDefinitionMeasure
    {
        string DefinitionJson { get; set; }
        MetricAssetDefinitionViewModel Definition { get; set; }
        MetricAssetDefinitionDataQualityViewModel DataQualityDefinition { get; set; }
    }

    [DataContract]
    public abstract class MetricBaseApiModel
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
        public float? Threshold { get; set; }

        [DataMember, JsonProperty(Order = 13)]
        public bool MatchConditionsOnly { get; set; } = false;

        [DataMember, JsonProperty(Order = 20)]
        public List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; } = new List<MetricAssetVersionConditionViewModel>();

        /// <summary>
        /// Used to help parse the json from the database.
        /// </summary>
        [DataMember]
        public string DefinitionJson { get; set; }

        /// <summary>
        /// Used to help parse the json from the database.
        /// </summary>
        [DataMember] 
        public MetricAssetDefinitionDataQualityViewModel DataQualityDefinition { get; set; }
    }


    [DataContract]
    public class MetricAssetViewModel : MetricBaseApiModel, IConditionGroupMeasure, IDefinitionMeasure
    {
        [DataMember, JsonProperty(Order = 21)]
        public int VersionCount { get; set; }

        [DataMember, JsonProperty(Order = 22)]
        public bool HasResults { get; set; } = false;

        [DataMember, JsonProperty(Order = 23)]
        public State State { get; set; }  

        [DataMember, JsonProperty(Order = 24)]
        public DateTime? EffectiveEndDate { get; set; }
    }

    [DataContract]
    public class MetricAssetEditModel : MetricBaseApiModel, IConditionGroupMeasure, IDefinitionMeasure
    {
        [IgnoreDataMember]
        public string CurrentConditionHash
        {
            get
            {
                var hashItems = from g in (ConditionGroups ?? new List<MetricAssetVersionConditionViewModel>())
                                from c in (g.ConditionItems ?? new List<MetricAssetVersionConditionItemViewModel>())
                                from v in (c.Values ?? new List<string>())
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

        public string GetHashValue()
        {
            string hash = "";

            if (DataQuality != null)
            {
                hash += "DQ:[";
                hash += $"FilterMatchType:{DataQuality.FilterMatchType}|";
                if (DataQuality.Filters != null)
                {
                    hash += $"Filters(";
                    DataQuality.Filters.OrderBy(f => f.AssetTypeID).ThenBy(f => f.FieldTypeName).ToList().ForEach(f =>
                    {
                        hash += $"AssetTypeID:{f.AssetTypeID}|FieldTypeID:{f.FieldTypeID}|Operator:{(int)f.Operator}|Values:{string.Join(";", f.Values.OrderBy(v => v))}|";
                    });
                    hash += $")|";
                }
                hash += $"ResultOperation:{DataQuality.ResultOperation}|";
                hash += $"ResultPathUid:{DataQuality.ResultPathUid}|";
                hash += "]";
            }
            if (Governance != null)
            {
                hash += "GOV:[";
                hash += $"Check:{Governance.Check}|";
                switch (Governance.Check)
                {
                    case MetricGovernanceCheckType.External:
                        if (Governance.External != null)
                        { 
                            hash += $"Instructions:{Governance.External.Instructions ?? ""}|";
                        }
                        break;
                    case MetricGovernanceCheckType.Field:
                        if (Governance.Field != null)
                        {
                            hash += $"FieldTypeName:{Governance.Field.FieldTypeName}|";
                            hash += $"Operator:{(int)Governance.Field.Operator}|";
                            if (Governance.Field.Values != null)
                            {
                                hash += $"Values:{string.Join(";", Governance.Field.Values.OrderBy(v => v))}|";
                            }
                        }
                        break;
                    case MetricGovernanceCheckType.Owner:
                        if (Governance.Owner != null)
                        {
                            hash += $"ResponsibilityTypeUid:{Governance.Owner.ResponsibilityTypeUid}|";
                            hash += $"Operator:{(int)Governance.Owner.Operator}|";
                        }
                        break;
                    case MetricGovernanceCheckType.Predicate:
                        if (Governance.Predicate != null)
                        {
                            hash += $"PredicateUid:{Governance.Predicate.PredicateUid}|";
                            hash += $"Operator:{(int)Governance.Predicate.Operator}|";
                        }
                        break;
                    case MetricGovernanceCheckType.Relation:
                        if (Governance.Relation != null)
                        {
                            hash += $"IntersectTypeUid:{Governance.Relation.IntersectTypeUid}|";
                            hash += $"Operator:{(int)Governance.Relation.Operator}|";
                            if (Governance.Relation.Values != null)
                            {
                                hash += $"Values:{string.Join(";", Governance.Relation.Values.OrderBy(v => v))}|";
                            }
                            
                        }
                        break;
                }
                hash += "]";
            }

            return hash.GetD3sHashString();
        }
    }

    #region DataQuality Definition Models

    [DataContract]
    public class MetricAssetDefinitionDataQualityViewModel
    {
        [DataMember] 
        public MetricRuleResultOperation ResultOperation { get; set; }
        [DataMember] 
        public Guid? ResultPathUid { get; set; }
        [DataMember] 
        public MetricMatchType? FilterMatchType { get; set; }
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

    public class MetricAssetDefinitionDataQualityFilterViewModelValueObject
    {
        public string Value { get; set; }
    }

    #endregion

    #region Governance Definition Models

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
        public List<string> Values { get; set; } = new List<string>();
    }

    [DataContract]
    public class MetricAssetDefinitionGovernancePredicateViewModel
    {
        [DataMember] 
        public Guid PredicateUid { get; set; }
        [DataMember] 
        public Operator Operator { get; set; }
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
        [DataMember]
        public Operator Operator { get; set; }
    }

    #endregion

    #region Perceptual Definition Models

    public class MetricAssetDefinitionPerceptualViewModel
    {
        public Guid QuestionTypeUid { get; set; }
        public int NumberOfSurveysToConsider { get; set; }
    }

    #endregion

    #region Rollup Definition Models

    public class MetricAssetDefinitionRollupViewModel
    {
        public MetricRuleResultOperation ResultOperation { get; set; }
        public bool CrossDescendancy { get; set; }
    }

    #endregion

    #region User Definition Models

    public class MetricAssetDefinitionUserViewModel
    {
        public MetricRuleResultOperation ResultOperation { get; set; }
    }

    #endregion

    [DataContract]
    public class MetricAssetViewDetailModel : MetricBaseApiModel, IConditionGroupMeasure, IDefinitionMeasure
    {
        [DataMember, JsonProperty(Order = 98)]
        public Guid AssetTypeUid { get; set; }

        [DataMember, JsonProperty(Order = 99)]
        public ScoreType ScoreType { get; set; }

        [DataMember, JsonProperty(Order = 100)]
        public List<MetricAssetVersionViewModel> Versions { get; set; }

        [IgnoreDataMember]
        public IEnumerable<MetricAssetVersionRollupPath> RollupPaths { get; set; }
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

    [DataContract]
    public class MetricFieldTypeViewModel
    {
        [DataMember]
        public Guid AssetTypeUid { get; set; }
        [DataMember] 
        public string AssetTypeName { get; set; }
        [DataMember] 
        public int ID { get; set; }
        [DataMember] 
        public string ApiName { get; set; }
        [DataMember] 
        public string Name { get; set; }
        [DataMember] 
        public string Type { get; set; }
        public string ValuesJson { get; set; }
        [DataMember]
        public List<MetricFieldTypeValueViewModel> Values 
        { 
            get 
            {
                return JsonConvert.DeserializeObject<List<MetricFieldTypeValueViewModel>>(ValuesJson ?? "[]");
            } 
        }
    }

    public class MetricFieldTypeValueViewModel
    {
        public Guid Value { get; set; }
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
        public string Path { get; set; }
    }

    public class MeasureVersionHistoryModel: IConditionGroupMeasure, IDefinitionMeasure
    {
        public Guid MeasureUid { get; set; }
        public int Version { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }        
        public DateTime EffectiveDate { get; set; }
        public DateTime? EffectiveEndDate { get; set; }
        public double Weight { get; set; }
        public float? Threshold { get; set; }
        public List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; } = new List<MetricAssetVersionConditionViewModel>();
        
        public bool HasResults { get; set; } = false;
        public bool MatchConditionsOnly { get; set; } = false;

        public string DefinitionJson { get; set; }
        public MetricAssetDefinitionViewModel Definition { get; set; }

        /// <summary>
        /// Used to help parse the json from the database.
        /// </summary>
        [DataMember]
        public MetricAssetDefinitionDataQualityViewModel DataQualityDefinition { get; set; }
    }

    #region Evidence Models

    public class DataQualityScoreItemEvidenceViewModel
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int total { get; set; }
        public List<DataQualityScoreItemEvidenceItemViewModel> items { get; set; }
    }

    public class DataQualityScoreItemEvidenceItemViewModel
    {
        [JsonIgnore]
        public string RollupPathJson { get; set; }

        public List<DataQualityScoreItemEvidenceItemRollupPathViewModel> RollupPath 
        { 
            get 
            { 
                return JsonConvert.DeserializeObject<List<DataQualityScoreItemEvidenceItemRollupPathViewModel>>(RollupPathJson ?? "[]"); 
            } 
        }

        public Guid? ResultUid { get; set; }
        public Guid OwningAssetUid { get; set; }
        public string OwningAssetPath { get; set; }
        public string OwningAssetTypePath { get; set; }
        public string OwningAssetDisplayPath { get; set; }
        public Guid EvaluatedAssetUid { get; set; }
        public string EvaluatedAssetPath { get; set; }
        public string EvaluatedAssetTypePath { get; set; }
        public string EvaluatedAssetDisplayPath { get; set; }
        public AssetTypeClass EvaluatedAssetClass { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? RunDate { get; set; }
        public int? TotalCount { get; set; }
        public float PassFraction { get; set; }
        public int? PassCount { get; set; }
        public int? FailCount { get; set; }
    }

    public class DataQualityScoreItemEvidenceItemRollupPathViewModel
    {
        public Guid Uid { get; set; }
        public string AssetPath { get; set; }
        public string AssetTypePath { get; set; }
        public string Predicate { get; set; }
        public int Position { get; set; }
    }

    #endregion
}
