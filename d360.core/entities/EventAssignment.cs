using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using d360.core.entities.Contracts;
namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class EventAssignment : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Active_Name", Description = "Active_Description")]
        public bool Active { get; set; }

        [DataMember]
        public int EventID { get; set; }

        [DataMember]
        public int? FromAssignmentID { get; set; }

        [DataMember]
        public string ResourceObjectType { get; set; }

        [DataMember]
        public int ResourceObjectID { get; set; }

        [IgnoreDataMember]
        public virtual Event Event { get; set; }
    }
}
