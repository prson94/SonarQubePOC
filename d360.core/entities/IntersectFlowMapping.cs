using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectFlowMapping : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public int IntersectFlowID { get; set; }

        [DataMember]
        public string Definition { get; set; }

        [DataMember]
        public string Formula { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        #endregion

        #region Navigation Properties

        [IgnoreDataMember]
        public IntersectFlow IntersectFlow { get; set; }

        [ForeignKey("IntersectFlowMappingID"), IgnoreDataMember]
        public virtual ICollection<IntersectFlowMappingItem> Items { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<DomainItem> Contexts { get; set; }

        #endregion


    }
}
