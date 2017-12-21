using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("ConditionValue", Schema = "metrics")]
    public class MetricConditionValue : BaseObject
    {
        [Key, Column(Order = 1)]
        public long MapID { get; set; }

        [Key, Column(Order = 2)]
        public int FieldTypeID { get; set; }

        [MaxLength(250), Key, Column(Order = 3)]
        public string Value { get; set; }

        [IgnoreDataMember]
        public virtual FieldType FieldType { get; set; }

        [IgnoreDataMember, ForeignKey("MapID, FieldTypeID")]
        public virtual MetricCondition MetricCondition { get; set; }

        [IgnoreDataMember]
        public virtual MetricMap Map { get; set; }
    }
}
