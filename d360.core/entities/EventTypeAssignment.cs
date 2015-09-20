using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class EventTypeAssignment : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EventTypeID { get; set; }

        [DataMember]
        public string ResourceObjectType { get; set; }

        [DataMember]
        public int ResourceObjectID { get; set; }

        [IgnoreDataMember]
        public virtual EventType EventType { get; set; }
    }
}
