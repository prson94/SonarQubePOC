using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class ScoreMetric : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ScoreID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ScoreTypeMetricVersionID { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [IgnoreDataMember]
        public virtual Score Score { get; set; }

        [IgnoreDataMember]
        public virtual ScoreTypeMetricVersion ScoreTypeMetricVersion { get; set; }
    }
}
