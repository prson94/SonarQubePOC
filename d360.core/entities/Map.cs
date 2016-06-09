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
        public string Name { get; set; }

        [DataMember]
        public int? IntersectRoleID { get; set; }

        [DataMember]
        public string Transformation { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual IntersectRole IntersectRole { get; set; }

        [DataMember]
        public virtual ICollection<MapItem> MapItems { get; set; }

        [DataMember]
        public virtual ICollection<MapRule> MapRules { get; set; }

        [DataMember]
        public virtual ICollection<MapSequence> MapSequences { get; set; }
    }
}
