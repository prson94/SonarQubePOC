using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ReferenceItemType : BaseIntObject, IIntObject, ISearchable, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "DisplayFormat_Name", Description = "DisplayFormat_Description")]
        public string DisplayFormat { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "SourceNotes_Name", Description = "SourceNotes_Description")]
        public string SourceNotes { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [ForeignKey("ReferenceItemTypeID"), IgnoreDataMember]
        public virtual ICollection<ReferenceItem> ReferenceItems { get; set; }
    }
}
