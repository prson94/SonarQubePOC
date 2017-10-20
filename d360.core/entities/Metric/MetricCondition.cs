using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Condition", Schema = "metrics")]
    public class MetricCondition : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int MetricMapID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int FieldTypeID { get; set; }

        [DataMember, StringLength(1)]
        public string AndOr { get; set; }

        [IgnoreDataMember]
        public virtual FieldType FieldType { get; set; }

        [IgnoreDataMember]
        public virtual MetricMap MetricMap { get; set; }
    }
}
