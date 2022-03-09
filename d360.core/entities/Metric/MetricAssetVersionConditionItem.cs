using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersionConditionItem", Schema = "metrics")]
    public class MetricAssetVersionConditionItem : BaseUidObject
    {
        [DataMember]
        public Guid AssetVersionConditionUid { get; set; }

        [DataMember]
        public MetricConditionType ConditionType { get; set; }

        [DataMember]
        public int? ConditionFieldTypeID { get; set; }

        [DataMember]
        public int? ConditionIntersectTypeID { get; set; }

        [DataMember]
        public Operator Operator { get; set; }

        [DataMember, ForeignKey("Uid")]
        public virtual ICollection<MetricAssetVersionConditionItemValue> Values { get; set; }

        // Used only during the measure update process to tell if this item has been touched. If not, it should be deleted.
        [NotMapped]
        public bool Updated { get; set; }

        [IgnoreDataMember, ForeignKey("Uid")]
        public virtual MetricAssetVersionCondition Condition { get; set; }

        [IgnoreDataMember, ForeignKey("ConditionFieldTypeID")]
        public virtual FieldType ConditionFieldType { get; set; }

        [IgnoreDataMember, ForeignKey("ConditionIntersectTypeID")]
        public virtual IntersectType ConditionIntersectType { get; set; }
    }
}
