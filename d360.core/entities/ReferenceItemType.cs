using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ReferenceItemType : BaseIntObject, IIntObject, ISearchable, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        [DataMember]
        public string Description { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [ForeignKey("ReferenceItemTypeID"), IgnoreDataMember]
        public virtual ICollection<ReferenceItem> ReferenceItems { get; set; }
    }
}
