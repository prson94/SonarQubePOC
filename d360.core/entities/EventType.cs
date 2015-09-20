using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.EventType, "EventType")]
    public class EventType : BaseIntObject, IIntObject
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public bool MarkAsResolvedOnSynch { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }


        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public virtual EventType Parent { get; set; }

        [ForeignKey("ParentID")]
        public virtual ICollection<EventType> Children { get; set; }

        [ForeignKey("EventTypeID")]
        public virtual ICollection<Event> Events { get; set; }
    }
}
