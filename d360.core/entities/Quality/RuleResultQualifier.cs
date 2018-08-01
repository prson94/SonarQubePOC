using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class RuleResultQualifier : BaseCreatedAndUpdatedObject
    {        
        [DataMember, Key, Column(Order = 1)]
        public int RuleResultID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int RuleResultQualifierTypeID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string ResolvedObject { get; set; }

        [DataMember]
        public int? ResolvedObjectID { get; set; }

        [IgnoreDataMember]
        public virtual RuleResult RuleResult { get; set; }

        [IgnoreDataMember]
        public virtual RuleResultQualifierType RuleResultQualifierType { get; set; }
    }
}
