using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AssetDataProfile", Schema = "dbo")]
    public class AssetDataProfile: BaseIntObject
    {
        [DataMember]
        public long AssetId { get; set; }
        [DataMember]
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
        [DataMember]
        public string ProcessIdentifier { get; set; }
    }

    public class AssetDataProfileViewModel
    {
        [DataMember]
        public Guid AssetUid { get; set; }
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
        public List<string> Top10Values { get; set; }
        [DataMember]
        public string ProcessIdentifier { get; set; }
    }
}

