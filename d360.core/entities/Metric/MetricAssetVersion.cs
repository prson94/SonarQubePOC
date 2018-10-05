using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersion", Schema = "metrics")]
    public class MetricAssetVersion : BaseCreatedObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember, StringLength(1)]
        public string ConditionAndOr { get; set; }

        [DataMember, ForeignKey("Uid")]
        public virtual MetricAsset Asset { get; set; }

        [DataMember, ForeignKey("Uid, EffectiveDate")]
        public virtual ICollection<MetricAssetVersionCondition> Conditions { get; set; }
    }
}