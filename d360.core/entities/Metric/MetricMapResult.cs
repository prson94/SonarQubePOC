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
        public int MetricMapID { get; set; }

        [Key, Column(Order = 2)]
        public int MetricScoreID { get; set; }

        public DateTime EffectiveStartDate { get; set; }

        public DateTime? EffectiveEndDate { get; set; }

        public bool Value { get; set; }

        [IgnoreDataMember]
        public virtual MetricMap MetricMap { get; set; }

        [IgnoreDataMember]
        public virtual MetricScore MetricScore { get; set; }
    }
}
