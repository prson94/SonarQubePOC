using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Asset", Schema = "metrics")]
    public class MetricAsset : BaseCreatedAndUpdatedObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public bool IsGroup { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [IgnoreDataMember, ForeignKey("ParentUid")]
        public virtual MetricAsset Parent { get; set; }

        [DataMember, ForeignKey("Uid")]
        public virtual ICollection<MetricAssetVersion> Versions { get; set; }

        [DataMember, ForeignKey("ParentUid")]
        public virtual ICollection<MetricAsset> Children { get; set; } 
    }
}
