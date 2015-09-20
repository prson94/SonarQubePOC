using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectNode : BaseIntObject, IIntObject
    {
        [DataMember]
        public int IntersectID { get; set; }

        [DataMember]
        public int IntersectTypeNodeID { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        public virtual Intersect Intersect { get; set; }

        public virtual IntersectTypeNode IntersectTypeNode { get; set; }
    }
}
