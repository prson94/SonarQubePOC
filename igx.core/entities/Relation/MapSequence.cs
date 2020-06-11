using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapSequence : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int MapItemID { get; set; }

        [DataMember]
        public int Sequence { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual MapItem MapItem { get; set; }

        [DataMember]
        public virtual ICollection<MapSequenceContext> MapSequenceContexts { get; set; }
    }
}
