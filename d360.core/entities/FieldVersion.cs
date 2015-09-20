using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldVersion : BaseObject
    {
        [Column(Order = 1), DataMember, Key, ReadOnly(true)]
        public LookupType ObjectType { get; set; }

        [Column(Order = 2), DataMember, Key, ReadOnly(true)]
        public int ObjectID { get; set; }

        [Column(Order = 3), DataMember, Key, ReadOnly(true)]
        public int FieldID { get; set; }

        [Column(Order = 4), DataMember, Key, ReadOnly(true)]
        public int Version { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DateCreated_Name", Description = "DateCreated_Description"), ReadOnly(true)]
        public DateTime DateCreated { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "CreatingResource_Name", Description = "CreatingResource_Description"), ReadOnly(true)]
        public int CreatingResourceID { get; set; }
    }
}
