using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AssetDataProfile", Schema = "dbo")]
    public class AssetDataProfile : BaseCreatedAndUpdatedLongObject
    {
        [DataMember]
        public long AssetId { get; set; }
        [DataMember]        
        public DateTime ProfileSetDate { get; set; }
        [DataMember]
        public long? TotalCount { get; set; }
        [DataMember]
        public long? DistinctValues { get; set; }
        [DataMember]
        public long? BlankCount { get; set; }
        [DataMember]
        public long? NullCount { get; set; }
        [DataMember]
        public string MinimumValue { get; set; }
        [DataMember]
        public string MaximumValue { get; set; }
        [DataMember]
        public Decimal? MeanValue { get; set; }
        [DataMember]
        public int MinimumLength { get; set; }
        [DataMember]
        public int MaximumLength { get; set; }
        [DataMember]
        public Decimal? StandardDeviation { get; set; }
        [DataMember]        
        public string NewType { get; set; }
        [DataMember]
        public bool? Multiline { get; set; }
        [DataMember]
        public string Validation { get; set; }
        [DataMember]
        public Decimal? Confidence { get; set; }
        [DataMember]
        public string TypeQualifier { get; set; }
        [DataMember]
        public string CurrentType { get; set; }
        [DataMember]
        public string JavaType { get; set; }
        [DataMember]
        public bool? LogicalType { get; set; }
        [DataMember]
        public bool? LeadingWhiteSpace { get; set; }
        [DataMember]
        public int? LeadingZeroCount { get; set; }
        [DataMember]
        public bool? TrailingWhiteSpace { get; set; }
        [DataMember]
        public long? MatchCount { get; set; }
        [DataMember]
        public int? OutlierCardinality { get; set; }
        [DataMember]
        public bool? PossibleKey { get; set; }
        [DataMember]
        public Decimal? PrimaryKey { get; set; }
        [DataMember]
        public string DataSignature { get; set; }
        [DataMember]
        public string StructureSignature { get; set; }
        [DataMember]
        public int? Cardinality { get; set; }
        [DataMember]
        public int? ShapeCardinality { get; set; }
        [ForeignKey("AssetDataProfileID"), IgnoreDataMember]
        public virtual ICollection<AssetDataProfileSample> AssetDataProfileSamples { get; set; }
    }

    [DataContract(Namespace = NAMESPACE), Table("AssetDataProfileSample", Schema = "dbo")]
    public class AssetDataProfileSample : BaseLongObject
    {
        [DataMember]
        public long AssetDataProfileID { get; set; }
        [DataMember]
        public string SampleType { get; set; }
        [DataMember]
        public string Key { get; set; }
        [DataMember]
        public string Value { get; set; }

    }
    public class DataProfileModel
    {
        [Required]
        public Guid assetUid { get; set; }

        [Required]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime profileSetDate { get; set; }

        public long? totalCount { get; set; }
        public long? distinctValues { get; set; }
        public long? blankCount { get; set; }
        public long? nullCount { get; set; }

        [StringLength(500, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string minValue { get; set; }

        [StringLength(500, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string maxValue { get; set; }       
        [Range(0, 999999999999999999.9999, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? meanValue { get; set; }
        public int? minLength { get; set; }
        public int? maxLength { get; set; }
        [Range(0, 999999999999999999999999.9999, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? standardDeviation { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string newType { get; set; }
        public bool? multiline { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string validation { get; set; }
        [Range(0, 999999999999999999999999.9999, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? confidence { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string typeQualifier { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string currentType { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string javaType { get; set; }
        public bool? logicalType { get; set; }
        public bool? leadingWhiteSpace { get; set; }
        public int? leadingZeroCount { get; set; }
        public bool? trailingWhiteSpace { get; set; }
        public long? matchCount { get; set; }
        public int? outlierCardinality { get; set; }
        public List<DataProfileSampleDetail> outlierDetail { get; set; }
        public bool? possibleKey { get; set; }
        [Range(0, 999999999999999999999999.9999, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? primaryKey { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string dataSignature { get; set; }
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string structureSignature { get; set; }
        public List<string> bottomK { get; set; }
        public List<string> topK { get; set; }
        public int? cardinality { get; set; }
        public List<DataProfileSampleDetail> cardinalityDetail { get; set; }
        public int? shapesCardinality { get; set; }
        public List<DataProfileSampleDetail> shapesDetail { get; set; }                
    }

    public class DataProfileUpsertModel: DataProfileModel, IExecutionItem
    {
        public Guid? ExecutionItemUid { get; set; }
    }

    public class DataProfileSampleDetail
    {
        public string key { get; set; }
        public int count { get; set; }
    }
}
