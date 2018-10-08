using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("MapResult", Schema = "metrics")]
    public class MetricMapResult : BaseObject
    {
        [Key, Column(Order = 1)]
        public int MapID { get; set; }

        [Key, Column(Order = 2)]
        public int ScoreID { get; set; }

        public bool Value { get; set; }

        [IgnoreDataMember]
        public virtual MetricMap Map { get; set; }

        [IgnoreDataMember]
        public virtual MetricScore Score { get; set; }
    }
}
