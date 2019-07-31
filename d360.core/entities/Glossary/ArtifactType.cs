using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ArtifactType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata, IEventTrackedEntity
    {
        #region Properties

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember, NotMapped]
        public int? AssetTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        [DataMember]
        public bool CanOwnFusion { get; set; }

        [DataMember]
        public bool AutoDisplayDescription { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }


        #endregion


        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.ArtifactType,
                ObjectID = ID,
                ObjectType = SystemObjects.ArtifactType,
                ObjectTypeID = 0
            };
        }

    }
}
