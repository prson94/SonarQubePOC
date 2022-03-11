using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("RollupPath", Schema = "metrics")]
    public class MetricRollupPath : BaseUidObject
    {
        [DataMember]
        public ScoreType ScoreType { get; set; }

        [DataMember]
        public State State { get; set; }

        [DataMember]
        public string PathHash { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember, ForeignKey("RollupPathUid")]
        public virtual ICollection<MetricRollupPathLink> Links { get; set; }

        [DataMember, ForeignKey("RollupPathUid")]
        public virtual ICollection<MetricRollupPathSegment> Segments { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), Table("RollupPathLink", Schema = "metrics")]
    public class MetricRollupPathLink : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid RollupPathUid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int StartPosition { get; set; }

        [DataMember]
        public int EndPosition { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [IgnoreDataMember, ForeignKey("RollupPathUid")]
        public virtual MetricRollupPath RollupPath { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), Table("RollupPathSegment", Schema = "metrics")]
    public class MetricRollupPathSegment : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid RollupPathUid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int Position { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [IgnoreDataMember, ForeignKey("RollupPathUid")]
        public virtual MetricRollupPath RollupPath { get; set; }
    }
}
