using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Item", Schema = "metrics")]
    public class MetricItem : BaseCreatedAndUpdatedLongObject
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateTime EffectiveStartDate { get; set; }

        public DateTime EffectiveEndDate { get; set; }

    }
}
