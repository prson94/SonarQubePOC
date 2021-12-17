using d360.core.entities.Contracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        public long? SampleCount { get; set; }        
        [DataMember]
        public long? BlankCount { get; set; }
        [DataMember]
        public long? NullCount { get; set; }
        [DataMember]
        public string MinimumValue { get; set; }
        [DataMember]
        public string MaximumValue { get; set; }
        [DataMember]
        public double? MeanValue { get; set; }
        [DataMember]
        public int? MinimumLength { get; set; }
        [DataMember]
        public int? MaximumLength { get; set; }
        [DataMember]
        public double? StandardDeviation { get; set; }
        [DataMember]
        public bool? Multiline { get; set; }
        [DataMember]
        public string RegExp { get; set; }
        [DataMember]
        public Decimal? Confidence { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string TypeQualifier { get; set; }
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
        public string DataSignature { get; set; }
        [DataMember]
        public string StructureSignature { get; set; }
        [DataMember]
        public int? Cardinality { get; set; }
        [DataMember]
        public int? ShapeCardinality { get; set; }        
        [DataMember]
        public long? TotalCount { get; set; }
        [DataMember]
        public long? OutlierCount { get; set; }
        [DataMember]
        public Decimal? KeyConfidence { get; set; }
        [DataMember]
        public string DetectionLocale { get; set; }
        [DataMember]
        public string FtaVersion { get; set; }
        [DataMember]
        public string DecimalSeparator { get; set; }

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
    [DataContract]
    public class DataProfileModel
    {
        [Required]
        [DataMember]
        public Guid assetUid { get; set; }

        [Required]
        [DataMember]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime profileSetDate { get; set; }

        [DataMember]
        public long? sampleCount { get; set; }
        
        [DataMember]
        public long? blankCount { get; set; }

        [DataMember]
        public long? nullCount { get; set; }

        [DataMember(Name = "min")]
        [StringLength(500, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string minValue { get; set; }

        [DataMember(Name = "max")]
        [StringLength(500, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string maxValue { get; set; }

        [DataMember(Name = "mean")]
        public double? meanValue { get; set; }

        [DataMember]
        public int? minLength { get; set; }

        [DataMember]
        public int? maxLength { get; set; }

        [DataMember]
        [Range(0, double.MaxValue, ErrorMessage = "{0} must be between {1} and {2}.")]
        public double? standardDeviation { get; set; }

        [DataMember]
        public bool? multiline { get; set; }

        [DataMember]
        public string regExp { get; set; }

        [DataMember]
        [Range(0, 1, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? confidence { get; set; }

        [DataMember]
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string type { get; set; }

        [DataMember]
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string typeQualifier { get; set; }

        [DataMember]
        public bool? logicalType { get; set; }

        [DataMember]
        public bool? leadingWhiteSpace { get; set; }

        [DataMember]
        public int? leadingZeroCount { get; set; }

        [DataMember]
        public bool? trailingWhiteSpace { get; set; }

        [DataMember]
        public long? matchCount { get; set; }

        [DataMember]
        public int? outlierCardinality { get; set; }

        [DataMember]
        [ValidateSampleAttribute(200)]
        public List<DataProfileSampleDetail> outlierDetail { get; set; }

        [DataMember]
        public bool? possibleKey { get; set; }

        [DataMember]
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string dataSignature { get; set; }

        [DataMember]
        [StringLength(200, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string structureSignature { get; set; }

        [DataMember]
        [ValidateKListAttribute(200)]
        public List<string> bottomK { get; set; }

        [DataMember]
        [ValidateKListAttribute(200)]
        public List<string> topK { get; set; }

        [DataMember]
        public int? cardinality { get; set; }

        [DataMember]
        [ValidateSampleAttribute(200)]
        public List<DataProfileSampleDetail> cardinalityDetail { get; set; }

        [DataMember]
        public int? shapesCardinality { get; set; }

        [DataMember]
        [ValidateSampleAttribute(200)]
        public List<DataProfileSampleDetail> shapesDetail { get; set; }

        [DataMember(Name = "totalCount")]
        public long? TotalCount { get; set; }

        [DataMember(Name = "outlierCount")]
        public long? OutlierCount { get; set; }

        [DataMember(Name = "keyConfidence")]
        [Range(0, 1, ErrorMessage = "{0} must be between {1} and {2}.")]
        [RegularExpression(@"^\d+.?\d{0,4}$", ErrorMessage = "{0} is limited to a maximum of 4 decimal places.")]
        public Decimal? KeyConfidence { get; set; }

        [DataMember(Name = "detectionLocale")]
        [StringLength(64, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string DetectionLocale { get; set; }

        [DataMember(Name = "ftaVersion")]
        [StringLength(32, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string FtaVersion { get; set; }

        [DataMember(Name = "decimalSeparator")]
        [StringLength(1, ErrorMessage = "{0} cannot be more than {1} characters.")]
        public string DecimalSeparator { get; set; }

        public DataProfileModel() { }

        public DataProfileModel(Guid uid, AssetDataProfile profile, List<AssetDataProfileSample> samples)
        {
            assetUid = uid;
            blankCount = profile.BlankCount;
            cardinality = profile.Cardinality;
            confidence = profile.Confidence;
            dataSignature = profile.DataSignature;
            DecimalSeparator = profile.DecimalSeparator;
            DetectionLocale = profile.DetectionLocale;
            FtaVersion = profile.FtaVersion;
            KeyConfidence = profile.KeyConfidence;
            leadingWhiteSpace = profile.LeadingWhiteSpace;
            leadingZeroCount = profile.LeadingZeroCount;
            logicalType = profile.LogicalType;
            matchCount = profile.MatchCount;
            maxLength = profile.MaximumLength;
            maxValue = profile.MaximumValue;
            meanValue = profile.MeanValue;
            minLength = profile.MinimumLength;
            minValue = profile.MinimumValue;
            multiline = profile.Multiline;
            nullCount = profile.NullCount;
            outlierCardinality = profile.OutlierCardinality;
            OutlierCount = profile.OutlierCount;
            profileSetDate = profile.ProfileSetDate;
            regExp = profile.RegExp;
            sampleCount = profile.SampleCount;
            shapesCardinality = profile.ShapeCardinality;
            standardDeviation = profile.StandardDeviation;
            structureSignature = profile.StructureSignature;
            TotalCount = profile.TotalCount;
            trailingWhiteSpace = profile.TrailingWhiteSpace;
            type = profile.Type;
            typeQualifier = profile.TypeQualifier;

            //samples
            this.shapesDetail = samples.Where((s) => s.SampleType.Equals("shapesdetail", StringComparison.InvariantCultureIgnoreCase)).Select((sd) => new DataProfileSampleDetail { key = sd.Key, count = int.Parse(sd.Value) }).ToList();
            this.outlierDetail = samples.Where((s) => s.SampleType.Equals("outlierdetail", StringComparison.InvariantCultureIgnoreCase)).Select((sd) => new DataProfileSampleDetail { key = sd.Key, count = int.Parse(sd.Value) }).ToList();
            this.cardinalityDetail = samples.Where((s) => s.SampleType.Equals("cardinalitydetail", StringComparison.InvariantCultureIgnoreCase)).Select((sd) => new DataProfileSampleDetail { key = sd.Key, count = int.Parse(sd.Value) }).ToList();
            this.topK = samples.Where((s) => s.SampleType.Equals("topk", StringComparison.InvariantCultureIgnoreCase)).Select((sd) => sd.Value).ToList();            
            this.bottomK = samples.Where((s) => s.SampleType.Equals("bottomk", StringComparison.InvariantCultureIgnoreCase)).Select((sd) => sd.Value).ToList();
            if (this.shapesDetail.Count==0)
            {
                this.shapesDetail = null;
            }
            if (this.outlierDetail.Count == 0)
            {
                this.outlierDetail = null;
            }
            if (this.cardinalityDetail.Count == 0)
            {
                this.cardinalityDetail = null;
            }
            if (this.topK.Count == 0)
            {
                this.topK = null;
            }
            if (this.bottomK.Count == 0)
            {
                this.bottomK = null;
            }
        }
    }

    public class DataProfileUpsertModel: DataProfileModel, IExecutionItem
    {
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
    }

    public class DataProfileSampleDetail
    {
        public string key { get; set; }
        public int count { get; set; }
    }

    public class AssetDataProfilesApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<DataProfileModel> items { get; set; }
    }

    public class AssetDataProfileDeleteModel: IExecutionItem
    {
        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public DateTime StartDate { get; set; }

        [DataMember]
        public DateTime EndDate { get; set; }

        [DataMember]
        public bool Cascade { get; set; }
        [DataMember]
        public Guid? ExecutionItemUid { get; set; }
    }

    [DataContract]
    public class AssetDataProfileMatchingAssetsModel
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public string path { get; set; }
        public string tagsJson { get; set; }
        public bool hasTagField { get; set; }
        [DataMember]
        public List<string> tags
        {
            get
            {
                return hasTagField ? JsonConvert.DeserializeObject<List<string>>(tagsJson ?? "[]") : null;
            }
        }
    }

    public class AssetDataProfilesMatchingAssetsApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<AssetDataProfileMatchingAssetsModel> items { get; set; }
    }

    public class AssetDataProfileByTypeQualifierModel
    {
        [DataMember]
        public Guid uid { get; set; }
        public string path { get; set; }
        public Decimal confidence { get; set; }
    }

    public class AssetDataProfileByTypeQualifierApiViewModel : PagedApiBaseViewModel
    {
        [DataMember]
        public IEnumerable<AssetDataProfileByTypeQualifierModel> items { get; set; }
    }

    public class DataProfileExportModel
    {
        public Guid AssetUid { get; set; }
        public long AssetID { get; set; }
        public string AssetTags { get; set; }
        public string AssetPath { get; set; }
        public string AssetTypePath { get; set; }
        public string MatchedAssetTags { get; set; }
        public string MatchedAssetPath { get; set; }
        public string MatchedAssetTypePath { get; set; }
        public Guid MatchedAssetUid { get; set; }
        public long MatchedAssetID { get; set; }
        public bool hasTagField { get; set; }
    }

    public class ValidateSampleAttribute : ValidationAttribute
    {
        public ValidateSampleAttribute(int maxlength)
        {
            Maxlength = maxlength;
        }

        public int Maxlength { get; }

        protected override ValidationResult IsValid(object value,
            ValidationContext validationContext)
        {
            var sample = (List<DataProfileSampleDetail>)value;

            if (sample?.Count>0 && sample.Any(x=>x.key?.Length > 200))
            {
                return new ValidationResult($"{validationContext.DisplayName} keys cannot be more than {Maxlength} characters.");
            }

            return ValidationResult.Success;
        }
    }

    public class ValidateKListAttribute : ValidationAttribute
    {
        public ValidateKListAttribute(int maxlength)
        {
            Maxlength = maxlength;
        }

        public int Maxlength { get; }

        protected override ValidationResult IsValid(object value,
            ValidationContext validationContext)
        {
            var list = (List<string>)value;

            if (list?.Count > 0 && list.Any(x => x.Length > 200))
            {
                return new ValidationResult($"{validationContext.DisplayName} elements cannot be more than {Maxlength} characters.");
            }

            return ValidationResult.Success;
        }
    }

}
