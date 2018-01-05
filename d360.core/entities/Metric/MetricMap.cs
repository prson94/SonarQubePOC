using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Map", Schema = "metrics")]
    public class MetricMap : BaseCreatedAndUpdatedLongObject
    {
        [DataMember]
        public int GroupID { get; set; }

        [DataMember]
        public long ItemID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public DateTime EffectiveStartDate { get; set; }

        [DataMember]
        public DateTime? EffectiveEndDate { get; set; }

        [IgnoreDataMember, NotMapped]
        public virtual MetricGroup Group { get; set; }

        [IgnoreDataMember, NotMapped]
        public virtual MetricItem Item { get; set; }
    }
}
