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
    public class EventHeader : BaseObject
    {
        [DataMember, ReadOnly(true), Key]
        public int EventGroupID { get; set; }

        [DataMember]
        public int EventTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int EventCount { get; set; }
    }
}
