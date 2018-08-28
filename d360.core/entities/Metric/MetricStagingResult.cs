using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("StagingResult", Schema = "metrics")]
    public class MetricStagingResult : BaseObject
    {
        [Key, Column(Order = 1)]
        public int MetricMapID { get; set; }

        [Key, Column(Order = 2)]
        public DateTime EffectiveDate { get; set; }

        [Key, Column(Order = 3)]
        public string Object { get; set; }

        [Key, Column(Order = 4)]
        public int ObjectID { get; set; }

        public bool Value { get; set; }

        public decimal Score { get; set; }

        public bool Processing { get; set; }

        [IgnoreDataMember]
        public virtual MetricMap MetricMap { get; set; }
    }
}
