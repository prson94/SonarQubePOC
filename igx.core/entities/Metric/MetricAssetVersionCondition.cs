using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersionCondition", Schema = "metrics")]
    public class MetricAssetVersionCondition : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public DateTime EffectiveDate { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int FieldTypeID { get; set; }

        [DataMember, StringLength(10)]
        public string Operator { get; set; }

        [IgnoreDataMember]
        public string ValueJson { get; set; }

        // Future use (mpappas) for when we start adding potentially multiple values that the JSON property above could store.
        [NotMapped, DataMember]
        public string Value {
            get {
                return ValueJson;
            }
        }

        [DataMember, ForeignKey("Uid, EffectiveDate")]
        public virtual MetricAssetVersion Version { get; set; } 
    }
}
