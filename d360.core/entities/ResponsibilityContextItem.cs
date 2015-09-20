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
    public class ResponsibilityContextItem : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ResponsibilityID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        [IgnoreDataMember]
        public virtual Responsibility Responsibility { get; set; }
    }
}
