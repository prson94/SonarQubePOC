using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("ScoreItem", Schema = "metrics")]
    public class ScoreItem : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid AssetUid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public Guid MetricAssetUid { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public DateTime EffectiveDate { get; set; }

        [NotMapped, DataMember]
        public Guid? AssetVersionUid { get; set; }

        [NotMapped, DataMember]
        public bool? BooleanResult { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [DataMember]
        public float? AdjustedWeight { get; set; }

        [DataMember]
        public DateTime? RunDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }
    }
}
