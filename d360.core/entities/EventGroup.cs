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
  [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.EventGroup, "EventGroup")]
    public class EventGroup : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        public int? RuleID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "PublicID_Name", Description = "PublicID_Description")]
        public string PublicID { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "EventCount_Name", Description = "EventCount_Description")]
        public int EventCount { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Rule Rule { get; set; }

        [ForeignKey("EventGroupID")]
        public virtual ICollection<Event> Events { get; set; }
    }
}
