using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapItem : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int SourceIntersectID { get; set; }

        [DataMember]
        public int TargetIntersectID { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [DataMember]
        public virtual ICollection<Map> Maps { get; set; }

        //[DataMember]
        //public virtual ICollection<MapRuleItem> MapRuleItems { get; set; }

        [IgnoreDataMember]
        public virtual Intersect SourceIntersect { get; set; }

        [IgnoreDataMember]
        public virtual Intersect TargetIntersect { get; set; }

        [DataMember]
        public virtual ICollection<MapSequence> MapSequences { get; set; }
    }
}
