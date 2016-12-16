using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectGroup : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int IntersectID { get; set; }

        [DataMember]
        public int GroupNumber { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Intersect Intersect { get; set; }


    }
}
