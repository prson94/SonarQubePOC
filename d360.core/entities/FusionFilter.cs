using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionFilter : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int FusionID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int FusionAttributeTypeID { get; set; }

        [DataMember]
        public string Filter { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttributeType FusionAttributeType { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
