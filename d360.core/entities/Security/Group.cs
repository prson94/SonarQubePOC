using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.queue;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Group : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "GroupPrimaryOwner_Name", Description = "GroupPrimaryOwner_Description")]
        public int? PrimaryOwnerResourceID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "GroupSecondaryOwner_Name", Description = "GroupSecondaryOwner_Description")]
        public int? SecondaryOwnerResourceID { get; set; }

        [DataMember, NotMapped]
        public Guid? PrimaryOwnerUid { get; set; }

        [DataMember, NotMapped]
        public Nullable<Guid> SecondaryOwnerUid { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "GroupIsActiveDirectory_Name", Description = "GroupIsActiveDirectory_Description")] 
        public bool IsActiveDirectoryGroup { get; set; } = false;

        [DataMember, NotMapped]
        public string PrimaryOwnerName { get; set; }

        [DataMember, NotMapped]
        public string SecondaryOwnerName { get; set; }
        [DataMember, NotMapped]
        public string UpdatedByName { get; set; }

        [DataMember, NotMapped]
        public string CreatedByName { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public Guid Uid { get; set; }
                
        public virtual ICollection<ResourceGroup> ResourceGroups { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Group,
                ObjectID = ID,
                ObjectType = SystemObjects.GroupType,
                ObjectTypeID = 0
            };
        }
    }
}
