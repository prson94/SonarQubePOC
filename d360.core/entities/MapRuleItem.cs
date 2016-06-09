using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapRuleItem : BaseIntObject, IIntObject
    {
        [DataMember]
        public int MapRuleID { get; set; }

        [DataMember]
        public int SourceFusionAttributeID { get; set; }

        [DataMember]
        public int TargetFusionAttributeID { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual MapRule MapRule { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttribute SourceFusionAttribute { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttribute TargetFusionAttribute { get; set; }
    }
}
