using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("RuleStepSetting", Schema = "fusion")]
    public class FusionRuleStepSetting : BaseObject
    {
            [DataMember, Key, Column(Order = 0)]
            public int RuleStepID { get; set; }
            
            
            [DataMember, Column(Order = 1), StringLength(100), Key]
            public string Name { get; set; }
            [DataMember, StringLength(250)]
            public string Value { get; set; }

            [ForeignKey("RuleStepID")]
            public virtual FusionRuleStep FusionRuleStep { get; set; }
            
    }    
}