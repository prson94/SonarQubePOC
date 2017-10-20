using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Map", Schema = "metrics")]
    public class MetricMap : BaseCreatedAndUpdatedIntObject
    {
        public int MetricGroupID { get; set; }

        public int MetricItemID { get; set; }

        public string Object { get; set; }

        public int? ObjectID { get; set; }

        public decimal Weight { get; set; }

        public DateTime EffectiveStartDate { get; set; }

        public DateTime? EffectiveEndDate { get; set; }

        [IgnoreDataMember]
        public virtual MetricGroup MetricGroup { get; set; }

        [IgnoreDataMember]
        public virtual MetricItem MetricItem { get; set; }
    }
}
