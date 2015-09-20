using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Xml.Linq;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeNode : BaseIntObject, IIntObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public short Order { get; set; }

        [DataMember]
        public string MenuDisplayText { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual IntersectType IntersectType { get; set; }

        [ForeignKey("IntersectTypeNodeID")]
        public virtual ICollection<IntersectNode> IntersectNodes { get; set; }
    }
}
