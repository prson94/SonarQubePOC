using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("DataProfile", Schema = "metrics")]
    public class MetricDataProfile : BaseCreatedAndUpdatedObject
    {
        [DataMember, Key, Column(Order = 1)]
        public Guid Uid { get; set; }

        public Guid AssetUid { get; set; }

        public int RowCount { get; set; }
        public decimal Uniqueness { get; set; }
        public int UniqueCount { get; set; }
        public decimal Completeness { get; set; }
        public int NullCount { get; set; }
        public int BlankCount { get; set; }
        public string DataType { get; set; }
        public string MinimumValue { get; set; }
        public string MaximumValue { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public decimal? Average { get; set; }
        public decimal? Median { get; set; }
        public decimal? StandardDeviation { get; set; }
        public string Top10Values { get; set; }

        public DateTime EffectiveDate { get; set; }
    }
}

