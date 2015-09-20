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
    public class FusionIntersectDetail : BaseObject
    {
        [DataMember]
        public int FusionAttributeID { get; set; }

        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int FusionIntersectTypeID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }
    }
}
