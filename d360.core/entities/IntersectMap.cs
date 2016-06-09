using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using d360.core.enums;
using System.Collections.Generic;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectMap : BaseIntObject, IIntObject
    {
        [DataMember]
        public int SubjectIntersectNodeID { get; set; }

        [DataMember]
        public int ObjectIntersectNodeID { get; set; }

        [DataMember]
        public int PredicateID { get; set; }

        [DataMember]
        public PredicateType Type { get; set; }

        [DataMember]
        public virtual ICollection<IntersectMapSourceRule> IntersectMapSourceRules { get; set; }
    }
}
