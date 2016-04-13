using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeNode : BaseIntObject, IIntObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }

        [DataMember]
        public short Order { get; set; }

        [DataMember, StringLength(250)]
        public string MenuDisplayText { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual IntersectType IntersectType { get; set; }

        [ForeignKey("IntersectTypeNodeID")]
        public virtual ICollection<IntersectNode> IntersectNodes { get; set; }
    }
}
