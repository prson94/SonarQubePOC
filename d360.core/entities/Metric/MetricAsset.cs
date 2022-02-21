using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Asset", Schema = "metrics")]
    public class MetricAsset : BaseCreatedAndUpdatedUidObject
    {
        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Guid AllocationUid { get; set; }

        [DataMember]
        public bool IsGroup { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [IgnoreDataMember, ForeignKey("AllocationUid")]
        public virtual MetricAllocation Allocation { get; set; }

        [IgnoreDataMember, ForeignKey("ParentUid")]
        public virtual MetricAsset Parent { get; set; }

        [DataMember, ForeignKey("AssetUid")]
        public virtual ICollection<MetricAssetVersion> Versions { get; set; }

        [DataMember, ForeignKey("ParentUid")]
        public virtual ICollection<MetricAsset> Children { get; set; }
    }
}
