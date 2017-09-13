using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Name = NAMESPACE)]
    public class FusionType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
        [
        DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), 
        Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired"), StringLength(250)
        ]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember, ForeignKey("FusionTypeID")]
        public virtual ICollection<Fusion> Fusions { get; set; }

        [IgnoreDataMember, ForeignKey("FusionTypeID")]
        public virtual ICollection<FusionAttributeType> FusionAttributeTypes { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.FusionType,
                ObjectID = ID,
                ObjectType = SystemObjects.FusionType,
                ObjectTypeID = 0
            };
        }
    }
}
