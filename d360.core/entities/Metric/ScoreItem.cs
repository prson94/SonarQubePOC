using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("ScoreItem", Schema = "metrics")]
    public class ScoreItem : BaseUidObject
    {
        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid MetricAssetUid { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public Guid? AssetVersionUid { get; set; }

        [DataMember]
        public Guid? ConditionUid { get; set; }

        [DataMember]
        public string Evidence { get; set; }

        [DataMember]
        public bool Value { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [DataMember]
        public decimal? AdjustedWeight { get; set; }

        [DataMember]
        public decimal? AdjustedMaxWeight { get; set; }

        [DataMember]
        public DateTime? RunDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }

        public ICollection<Score> Scores { get; set; }

        [IgnoreDataMember, NotMapped]
        public decimal? RawMeasureWeight { get; set; }
    }
}
