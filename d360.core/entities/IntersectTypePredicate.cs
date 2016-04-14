using System.Runtime.Serialization;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypePredicate : BaseIntObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public MapType PredicateType { get; set; }

        //[DataMember]
        //public int PredicateID { get; set; }

        public virtual IntersectType IntersectType { get; set; }

        //public virtual Predicate Predicate { get; set; }
    }
}
