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
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Guid AllocationUid { get; set; }

        [DataMember]
        public Guid? AssetTypeUid { get; set; }

        [DataMember]
        public ScoreType? ScoreType { get; set; }

        [DataMember]
        public bool IsGroup { get; set; }

        [DataMember]
        [Required(AllowEmptyStrings = false, ErrorMessage = "You have provided an invalid name.")]
        [MaxLength(250, ErrorMessage = "{0} cannot exceed {1} characters.")]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public float Threshold { get; set; }

        [DataMember]
        public MetricUpdateFrequency UpdateFrequency { get; set; } = MetricUpdateFrequency.None;

        [DataMember]
        public bool MatchConditionsOnly { get; set; } = false;

        [DataMember]
        public List<MetricAssetVersionConditionViewModel> ConditionGroups { get; set; } = new List<MetricAssetVersionConditionViewModel>();
    }

    [DataContract]
    public class MetricAssetVersionConditionViewModel
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public int Position { get; set; } = 1;

        [DataMember]
        public float? Threshold { get; set; }

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
        
        [DataMember]
        public int? ConditionFieldTypeID{ get; set; }
        
        [DataMember]
        public int? ConditionIntersectTypeID { get; set; }

        [DataMember, StringLength(10)]
        public string Operator { get; set; }

        [DataMember]
        public List<MetricAssetVersionConditionItemValue> Values { get; set; }

    }

    public class MetricFieldTypeViewModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<MetricFieldTypeValueViewModel> Values { get; set; }
    }

    public class MetricFieldTypeValueViewModel
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}
