using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Condition", Schema = "metrics")]
    public class MetricCondition : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long MapID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int FieldTypeID { get; set; }

        [DataMember, StringLength(1)]
        public string AndOr { get; set; }

        [DataMember, StringLength(10)]
        public string Operator { get; set; }

        [DataMember]
        public string Value { get; set; }

        [IgnoreDataMember, NotMapped]
        public virtual FieldType FieldType { get; set; }

        [IgnoreDataMember, NotMapped]
        public virtual MetricMap Map { get; set; }
    }
}
