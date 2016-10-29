using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace d360.core.entities
{
    public class RuleModel : BaseIntObject
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
        public RuleStatus Status { get; set; }

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
    public class Rule : BaseUpdatedIntObject, ICreatedObject, IUpdatedObject
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
        public RuleStatus Status { get; set; }

        [DataMember]
        public RuleType RuleType { get; set; }

        [DataMember]
        public decimal Threshold { get; set; }

        [DataMember]
        public int? RuleDimensionID { get; set; }

        [DataMember, ForeignKey("RuleDimensionID")]
        public RuleDimension Dimension { get; set; }

        [ForeignKey("RuleID")]
        public virtual ICollection<RuleResult> Results { get; set; }

        [ForeignKey("RuleID")]
        public virtual ICollection<RuleMap> Maps { get; set; }
    }
}
