using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("ScoreItem", Schema = "metrics")]
    public class ScoreItem : BaseUidObject
    {
        [DataMember]
        public Guid AssetVersionUid { get; set; }

        [DataMember]
        public Guid? ConditionUid { get; set; }

        [DataMember]
        public string OtherConditions { get; set; }

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

        public ICollection<Score> Scores { get; set; }

        [IgnoreDataMember, NotMapped]
        public decimal? RawMeasureWeight { get; set; }

        [IgnoreDataMember, NotMapped]
        public Guid MetricAssetUid { get; set; }

        /// <summary>
        /// Used for data quality measures when the score allocation is not threshold-based. 
        /// We use this value to apply to the adjustedmaxweight in order to get a percentage 
        /// of that to then contribute towards the score.
        /// </summary>
        [IgnoreDataMember]
        public float? DecimalValue { get; set; }
    }
}
