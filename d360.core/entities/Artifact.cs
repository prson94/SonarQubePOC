using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Artifact, "Artifact")]
    public class Artifact : BaseIntObject, IIntObject, IFieldsObject, ICreatedObject, IUpdatedObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ArtifactType_Name", Description = "ArtifactType_Description")]
        public int ArtifactTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Definition_Name", Description = "Definition_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }
        
        //[ReadOnly(true)]
        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        //public string Path { get; set; }

        [DataMember]
        [ReadOnly(true)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string TextPath { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Status_Name", Description = "Status_Description")]
        public string Status { get; set; }

        [DataMember]
        public int TaxonomyTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DateLastCertified_Name", Description = "DateLastCertified_Description")]
        public DateTime? DateLastCertified { get; set; }

        #endregion

        #region Navigation Properties

        [IgnoreDataMember]
        public virtual ArtifactType ArtifactType { get; set; }

        [IgnoreDataMember]
        public virtual TaxonomyType TaxonomyType { get; set; }

        [IgnoreDataMember]
        public virtual Artifact Parent { get; set; }

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<Artifact> Children { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<Fusion> OwnedFusions { get; set; }

        #endregion

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        public DateTime CreatedOn
        {
            get
            {
                return this.createdon.HasValue
                   ? this.createdon.Value
                   : DateTime.UtcNow;
            }

            set { this.createdon = value; }
        }

        private DateTime? createdon = null;
    }
}
