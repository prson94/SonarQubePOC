using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Group", Schema = "metrics")]
    public class MetricGroup : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [DataMember, ForeignKey("ParentID")]
        public virtual ICollection<MetricGroup> Children { get; set; } 
    }
}
