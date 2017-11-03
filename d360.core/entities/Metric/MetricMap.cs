using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Map", Schema = "metrics")]
    public class MetricMap : BaseCreatedAndUpdatedLongObject
    {
        public int GroupID { get; set; }

        public int ItemID { get; set; }

        public string Object { get; set; }

        public int ObjectID { get; set; }

        public decimal Weight { get; set; }

        public DateTime EffectiveStartDate { get; set; }

        public DateTime EffectiveEndDate { get; set; }

        [IgnoreDataMember]
        public virtual MetricGroup Group { get; set; }

        [IgnoreDataMember]
        public virtual MetricItem Item { get; set; }
    }
}
