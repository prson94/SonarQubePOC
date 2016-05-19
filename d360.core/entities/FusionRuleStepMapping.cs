using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("RuleStepMapping", Schema = "fusion")]
    public class FusionRuleStepMapping : BaseIntObject
    {
        [DataMember]
        public int RuleStepID { get; set; }

        [DataMember, StringLength(250)]
        public string SourceFieldName { get; set; }

        [DataMember]
        public int SourceFieldTypeID { get; set; }

        [DataMember, StringLength(250)]
        public string TargetFieldName { get; set; }

        [DataMember]
        public int TargetFieldTypeID { get; set; }

        [DataMember]
        public bool IsConstantValue { get; set; }

        [DataMember, StringLength(250)]
        public string ConstantValue { get; set; }


        [ForeignKey("RuleStepID")]
        public virtual FusionRuleStep FusionRuleStep { get; set; }
    }
}