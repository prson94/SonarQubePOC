using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class RuleModel : BaseIntObject, IIntObject
    {
        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Purpose { get; set; }

        [DataMember]
        public string Measurement { get; set; }

        [DataMember]
        public string Resolution { get; set; }

        [DataMember]
        public enums.RuleStatus Status { get; set; }

        [DataMember]
        public RuleType RuleType { get; set; }

        [DataMember]
        public decimal? Threshold { get; set; }

        [DataMember]
        public int? RuleDimensionID { get; set; }

        [DataMember]
        public string SourceID { get; set; }
        
    }


    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Rule, "Rule")]
    public class Rule : BaseCreatedAndUpdatedIntObject, IIntObject, ICreatedObject, IUpdatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        public Rule()
        {
            Visible = true;
        }

        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Purpose { get; set; }

        [DataMember]
        public string Measurement { get; set; }

        [DataMember]
        public string Resolution { get; set; }

        [DataMember]
        public enums.RuleStatus Status { get; set; }

        [DataMember]
        public decimal Threshold { get; set; }

        [DataMember]
        public int? RuleDimensionID { get; set; }

        [DataMember]
        public int RuleTypeID { get; set; }

        [DataMember, ForeignKey("RuleTypeID")]
        public RuleType RuleType { get; set; }

        [DataMember, ForeignKey("RuleDimensionID")]
        public RuleDimension Dimension { get; set; }

        public bool Visible { get; set; }

        [ForeignKey("RuleID")]
        public virtual ICollection<RuleResult> Results { get; set; }

        [ForeignKey("RuleID")]
        public virtual ICollection<RuleMap> Maps { get; set; }

        [ForeignKey("RuleID"), IgnoreDataMember]
        public virtual ICollection<RuleResultQualifierType> RuleResultQualifierTypes { get; set; }
    }
}
