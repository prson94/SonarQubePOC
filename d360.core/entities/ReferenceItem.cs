using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ReferenceItem : BaseIntObject, IIntObject, ISearchable, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string DisplayValue { get; set; }

        [DataMember]
        public int ReferenceItemTypeID { get; set; }

        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual ReferenceItemType ReferenceItemType { get; set; }
    }
}
