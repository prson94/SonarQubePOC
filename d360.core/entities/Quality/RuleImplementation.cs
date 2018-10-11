using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleImplementation : BaseCreatedAndUpdatedIntObject, ICreatedObject, IUpdatedObject, IUpdatedMetadata
    {        
        [DataMember]
        public int RuleID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string SourceID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(2500)]
        public string SourceUri { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }

        [ForeignKey("RuleImplementationID")]
        public virtual ICollection<RuleResult> Results { get; set; }

        [ForeignKey("RuleImplementationID"), IgnoreDataMember]
        public virtual ICollection<RuleResultQualifierType> RuleResultQualifierTypes { get; set; }
    }
}
