using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class EventTypeRelation : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int EventTypeID { get; set; }

        [DataMember]
        public string EventTypeName { get; set; }
    }
}
