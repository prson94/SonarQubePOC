using d360.core.enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Group", Schema = "metrics")]
    public class MetricGroup : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public DateTime EffectiveStartDate { get; set; }

        [DataMember]
        public DateTime? EffectiveEndDate { get; set; }

        [DataMember]
        public string SourceID { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

    }
}
