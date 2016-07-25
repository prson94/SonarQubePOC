using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapRuleItem : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public string SourceOwner { get; set; }
        [DataMember]
        public int? SourceOwnerID { get; set; }
        [DataMember]
        public int SourceFusionAttributeID { get; set; }

        [DataMember]
        public string TargetOwner { get; set; }
        [DataMember]
        public int? TargetOwnerID { get; set; }
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

        [DataMember]
        public virtual ICollection<MapRule> MapRules { get; set; }

        //[DataMember]
        //public virtual ICollection<MapItem> MapItems { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttribute SourceFusionAttribute { get; set; }

        [IgnoreDataMember]
        public virtual FusionAttribute TargetFusionAttribute { get; set; }
    }
}
