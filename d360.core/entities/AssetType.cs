using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetType : BaseIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember]
        public bool Hierarchical { get; set; }

        [DataMember]
        public int? HierarchyIntersectTypeID { get; set; }

        [DataMember]
        public int? HierarchyPredicateID { get; set; }

        [DataMember]
        public int HierarchyMaximumDepth { get; set; }
        


        //[IgnoreDataMember, ForeignKey("IntersectTypeID")]
        //public virtual Predicate HierarchyIntersectType { get; set; }

        //[IgnoreDataMember, ForeignKey("PredicateID")]
        //public virtual Predicate HierarchyPredicate { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeID")]
        public virtual ICollection<Asset> Assets { get; set; }
    }
}
