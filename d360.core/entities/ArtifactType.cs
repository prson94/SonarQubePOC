using d360.core.entities.Contracts;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.ArtifactType, "ArtifactType")]
    public class ArtifactType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AllowHierarchy_Name", Description = "AllowHierarchy_Description")]
        public bool AllowHierarchy { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AllowRelatedArtifacts_Name", Description = "AllowRelatedArtifacts_Description")]
        public bool AllowRelatedArtifacts { get; set; }
        
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "CanOwnFusion_Name", Description = "CanOwnFusion_Description")]
        public bool CanOwnFusion { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember, ForeignKey("ParentID")]
        public virtual ArtifactType Parent { get; set; }

        #endregion

        #region Collection Properties

        [IgnoreDataMember, ForeignKey("ArtifactTypeID")]
        public virtual ICollection<Artifact> Artifacts { get; set; }

        #endregion

    }
}
