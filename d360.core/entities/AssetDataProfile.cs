using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AssetDataProfile", Schema = "dbo")]
    public class AssetDataProfile : BaseCreatedAndUpdatedObject
    {
        [DataMember, Key, Column(Order = 1)]
        public long AssetId { get; set; }
        [DataMember, Key, Column(Order = 2)]
        public DateTime EffectiveDate { get; set; }
        [DataMember]
        public int RowCount { get; set; }
        [DataMember]
        public decimal Uniqueness { get; set; }
        [DataMember]
        public int UniqueCount { get; set; }
        [DataMember]
        public decimal Completeness { get; set; }
        [DataMember]
        public int NullCount { get; set; }
        [DataMember]
        public int BlankCount { get; set; }
        [DataMember]
        public string DataType { get; set; }
        [DataMember]
        public string MinimumValue { get; set; }
        [DataMember]
        public string MaximumValue { get; set; }
        [DataMember]
        public int? Precision { get; set; }
        [DataMember]
        public int? Scale { get; set; }
        [DataMember]
        public decimal? Average { get; set; }
        [DataMember]
        public decimal? Median { get; set; }
        [DataMember]
        public decimal? StandardDeviation { get; set; }
        [DataMember]
        public string Top10Values { get; set; }
        public string ProcessIdentifier { get; set; }
    }
}

