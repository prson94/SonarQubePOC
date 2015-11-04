using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.DomainGroup, "DomainGroup")]
    public class DomainGroup : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [Required(AllowEmptyStrings = false, ErrorMessageResourceType = typeof(d360.core.resources.Fields), ErrorMessageResourceName = "Name_ErrorRequired")]
        [StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description")]
        public int DomainTypeID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MasterList_Name", Description = "MasterList_Description")]
        public int? MasterListID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Type_Name", Description = "Type_Description"), IgnoreDataMember]
        public virtual DomainType DomainType { get; set; }

        //[ForeignKey("MasterListID")]
        //public virtual DomainList MasterList { get; set; }

        [ForeignKey("DomainGroupID"), IgnoreDataMember]
        public virtual ICollection<Domain> Items { get; set; }
    }
}
