using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class Score : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public int ScoreTypeID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        public int Value { get; set; }

        [IgnoreDataMember]
        public virtual ScoreType ScoreType { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<ScoreMetric> ScoreMetrics { get; set; }
    }
}
