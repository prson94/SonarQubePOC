using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapItem : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public int MapID { get; set; }

        [DataMember]
        public int IntersectID { get; set; }

        [DataMember]
        public bool IsSource { get; set; }

        [DataMember]
        public string DiagramKey { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Map Map { get; set; }

        [IgnoreDataMember]
        public virtual Intersect Intersect { get; set; }
    }
}
