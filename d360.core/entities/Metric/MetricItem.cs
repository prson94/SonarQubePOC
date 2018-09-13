using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Item", Schema = "metrics")]
    public class MetricItem : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime EffectiveStartDate { get; set; }

        [DataMember]
        public DateTime? EffectiveEndDate { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        public string Description { get; set; }
    }
}
