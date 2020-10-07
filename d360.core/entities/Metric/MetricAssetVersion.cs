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
        [DataMember, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uid { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        [Required(AllowEmptyStrings = false, ErrorMessage = "You have provided an invalid Name.")]
        [MaxLength(250, ErrorMessage = "Name cannot exceed 250 characters.")]
        public string Name { get; set; }

        [DataMember]
        public string Definition { get; set; } = "{}";

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public double? Threshold { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public MetricUpdateFrequency UpdateFrequency { get; set; } = MetricUpdateFrequency.None;

        [DataMember]
        public bool MatchConditionsOnly { get; set; } = false;

        [DataMember, StringLength(1)]
        public string ConditionAndOr { get; set; }

        [DataMember]
        public DateTime? EffectiveEndDate { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [IgnoreDataMember, ForeignKey("Uid")]
        public virtual MetricAsset Asset { get; set; }

        [DataMember, ForeignKey("AssetVersionUid")]
        public virtual ICollection<MetricAssetVersionCondition> Conditions { get; set; }

        [DataMember, ForeignKey("AssetVersionUid")]
        public virtual ICollection<MetricAssetVersionRollupPath> RollupPaths { get; set; }
    }
}