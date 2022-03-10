using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersionCondition", Schema = "metrics")]
    public class MetricAssetVersionCondition : BaseUidObject
    {
        [DataMember]
        public Guid AssetVersionUid { get; set; }

        [DataMember]
        public MetricMatchType MatchType { get; set; }

        [DataMember]
        public int Position { get; set; }

        [DataMember]
        public double? Threshold { get; set; }

        [DataMember]
        public decimal? Weight { get; set; }

        // Used only during the measure update process to tell if this item has been touched. If not, it should be deleted.
        [NotMapped]
        public bool Updated { get; set; }

        [IgnoreDataMember, ForeignKey("Uid")]
        public virtual MetricAssetVersion Version { get; set; }

        [DataMember, ForeignKey("AssetVersionConditionUid")]
        public virtual ICollection<MetricAssetVersionConditionItem> Items { get; set; }
    }
}
