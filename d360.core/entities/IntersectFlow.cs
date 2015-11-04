using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectFlow : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int IntersectFlowTypeID { get; set; }

        [DataMember]
        public string Formula { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }


        [IgnoreDataMember, ForeignKey("IntersectFlowTypeID")]
        public IntersectFlowType IntersectFlowType { get; set; }

        [IgnoreDataMember, ForeignKey("IntersectFlowID")]
        public virtual ICollection<IntersectFlowItem> IntersectFlowItems { get; set; }

        [IgnoreDataMember, ForeignKey("IntersectFlowID")]
        public virtual ICollection<IntersectFlowMapping> IntersectFlowMappings { get; set; }
    }
}
