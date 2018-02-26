using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class AssetType : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public AssetTypeClass Class { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

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

        [DataMember]
        public string Notes { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeID")]
        public virtual ICollection<Asset> Assets { get; set; }
    }
}
