using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleResult : BaseCreatedIntObject, IIntObject, ICreatedObject
    {        
        [DataMember]
        public int RuleImplementationID { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public DateTime RunDate { get; set; }

        [DataMember]
        public int RowsPassed { get; set; }

        [DataMember]
        public int RowsFailed { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal PassFraction { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal FailFraction { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public bool Passed { get; set; }

        [IgnoreDataMember]
        public virtual RuleImplementation RuleImplementation { get; set; }
                
        [ForeignKey("RuleResultID"), IgnoreDataMember]
        public virtual ICollection<RuleResultFusionAttribute> RuleResultFusionAttributes { get; set; }
    }
}
