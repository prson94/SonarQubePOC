using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("RuleItem", Schema = "fusion")]
    public class FusionRuleItem : BaseIntObject
    {
        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }
        
        [DataMember]
        public string ObjectType { get; set; }
                
        [ForeignKey("RuleID")]
        public virtual FusionRule FusionRule { get; set; }
    }
}
