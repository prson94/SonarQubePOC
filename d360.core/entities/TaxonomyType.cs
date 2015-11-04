using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.TaxonomyType, "TaxonomyType")]
    public class TaxonomyType : BaseIntObject, IIntObject, ISearchable, IUpdatedMetadata
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
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "MaximumDepth_Name", Description = "MaximumDepth_Description")]
        public int? MaximumDepth { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Class_Name", Description = "Class_Description")]
        public int TaxonomyTypeClassID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Collection Properties

        [IgnoreDataMember, ForeignKey("TaxonomyTypeClassID")]
        public virtual TaxonomyTypeClass TaxonomyTypeClass { get; set; }

        [IgnoreDataMember, ForeignKey("TaxonomyTypeID")]
        public virtual ICollection<Taxonomy> Taxonomies { get; set; }

        [IgnoreDataMember, ForeignKey("TaxonomyTypeID")]
        public virtual ICollection<TaxonomyTypeLevel> TaxonomyTypeLevels { get; set; }

        #endregion
    }
}
