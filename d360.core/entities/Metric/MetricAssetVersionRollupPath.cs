using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("AssetVersionRollupPath", Schema = "metrics")]
    public class MetricAssetVersionRollupPath : BaseUidObject
    {
        [DataMember]
        public Guid AssetVersionUid { get; set; }

        [DataMember]
        public Guid RollupPathUid { get; set; }

        [DataMember]
        public MetricMatchType FilterMatchType { get; set; }

        [DataMember, ForeignKey("AssetVersionRollupPathUid")]
        public virtual ICollection<MetricAssetVersionRollupPathFilter> Filters { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), Table("AssetVersionRollupPathFilter", Schema = "metrics")]
    public class MetricAssetVersionRollupPathFilter : BaseUidObject
    {
        [DataMember]
        public Guid AssetVersionRollupPathUid { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public Operator Operator { get; set; }

        [DataMember, ForeignKey("AssetVersionRollupPathFilterUid")]
        public virtual ICollection<MetricAssetVersionRollupPathFilterValue> Values { get; set; }

        [IgnoreDataMember, ForeignKey("AssetVersionRollupPathUid")]
        public virtual MetricAssetVersionRollupPath AssetVersionRollupPath { get; set; }

        [IgnoreDataMember, ForeignKey("AssetTypeID")]
        public virtual AssetType AssetType { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual FieldType FieldType { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), Table("AssetVersionRollupPathFilterValue", Schema = "metrics")]
    public class MetricAssetVersionRollupPathFilterValue : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid AssetVersionRollupPathFilterUid { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public string Value { get; set; }

        [IgnoreDataMember, ForeignKey("AssetVersionRollupPathFilterUid")]
        public virtual MetricAssetVersionRollupPathFilter AssetVersionRollupPathFilter { get; set; }
    }
}
