using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Score", Schema = "metrics")]
    public class MetricScore : BaseLongObject
    {
        public string Object { get; set; }

        public int ObjectID { get; set; }

        public DateTime EffectiveStartDate { get; set; }

        public DateTime? EffectiveEndDate { get; set; }

        public decimal Value { get; set; }

    }
}
