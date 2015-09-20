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
    public class AlertFlag : BaseIntObject, IIntObject
    {
        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int CommentID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public bool Active { get; set; }
    }
}
