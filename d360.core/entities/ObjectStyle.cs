using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ObjectStyle : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember]
        public string IconBackColor { get; set; }

        [DataMember]
        public string IconForeColor { get; set; }

        [DataMember]
        public string IconText { get; set; }
    }
}
