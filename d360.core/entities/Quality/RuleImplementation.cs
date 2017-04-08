using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleImplementation : BaseCreatedAndUpdatedIntObject
    {        
        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SourceUri { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }

        [ForeignKey("RuleImplementationID")]
        public virtual ICollection<RuleResult> Results { get; set; }

        [ForeignKey("RuleImplementationID"), IgnoreDataMember]
        public virtual ICollection<RuleResultQualifierType> RuleResultQualifierTypes { get; set; }
    }
}
