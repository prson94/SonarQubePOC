using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Domain, "Domain")]
    public class Domain : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int DomainTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Group_Name", Description = "Group_Description")]
        public int? DomainGroupID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "EnforceParentItemSelection_Name", Description = "EnforceParentItemSelection_Description")]

        public bool EnforceParentItemSelection { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description")]
        public int? ParentID { get; set; }

        [ReadOnly(true)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Path_Name", Description = "Path_Description")]
        public string Path { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #region Navigation Properties

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Group_Name", Description = "Group_Description"), IgnoreDataMember]
        public virtual DomainGroup DomainGroup { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description"), IgnoreDataMember]
        public virtual DomainType DomainType { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Parent_Name", Description = "Parent_Description"), IgnoreDataMember]
        public virtual Domain Parent { get; set; }

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<Domain> Children { get; set; }

        [ForeignKey("DomainID"), IgnoreDataMember]
        public virtual ICollection<DomainItem> Items { get; set; }

        #endregion
    }
}
