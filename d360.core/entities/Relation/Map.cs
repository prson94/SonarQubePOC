using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Map : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int MapTypeID { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual MapType MapType { get; set; }

        [DataMember]
        public int? MapTypeTemplateID { get; set; }

        [DataMember]
        public virtual ICollection<MapItem> MapItems { get; set; }
    }
}
