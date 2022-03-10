using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

using Newtonsoft.Json;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Score", Schema = "metrics")]
    public class Score : BaseUidObject
    {
        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid AllocationUid { get; set; }

        [DataMember]
        public DateTime EffectiveDate { get; set; }

        [DataMember]
        public decimal Value { get; set; }

        [DataMember]
        public DateTime? RunDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }

        public string VersionValueHash { get; set; }

        public string Log { get; set; }

        public ICollection<ScoreItem> Items { get; set; }
    }

    #region API Models

    public abstract class BaseScoreResultApiRequestModel
    {
        [DataMember]
        public Guid assetUid { get; set; }

        [DataMember]
        public DateTime? effectiveDate { get; set; }

        #region Populated internally

        [IgnoreDataMember]
        public Guid? allocationUid { get; set; } = null;

        [IgnoreDataMember]
        public ScoreType? scoreType { get; set; } = null;

        #endregion
    }

    public class ExternalScoreResultApiRequestModel : BaseScoreResultApiRequestModel
    {
        [DataMember]
        public decimal score { get; set; }

        [DataMember]
        public DateTime? runDate { get; set; }

        public List<ExternalScoreResultMeasureModel> measures { get; set; }
    }

    public class ExternalScoreResultMeasureModel
    {
        [DataMember]
        public Guid measureUid { get; set; }

        [DataMember]
        public bool passed { get; set; }
    }

    public class ExternalScoreResultApiResponseModel
    {
        [JsonIgnore]
        public Guid ScoreUid { get; set; }

        public Guid AllocationUid { get; set; }

        public Guid AssetUid { get; set; }

        public decimal Score { get; set; }

        public DateTime RunDate { get; set; }

        public DateTime EffectiveDate { get; set; }

        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; }

        public List<ExternalScoreResultMeasureModel> Measures { get { return JsonConvert.DeserializeObject<List<ExternalScoreResultMeasureModel>>((string.IsNullOrEmpty(measuresJson)) ? "[]" : measuresJson); } }
        
        [JsonIgnore]
        public string measuresJson { get; set; }
    }

    public class InternalScoreResultApiRequestModel : BaseScoreResultApiRequestModel
    {
        [DataMember]
        public Guid metricAssetUid { get; set; }

        [DataMember]
        public bool result { get; set; }
    }

    [DataContract(Name = "metric")]
    public class InternalScoreResultApiResponseModel : BaseObject
    {
        [DataMember]
        public Guid AssetUid { get; set; }

        [DataMember]
        public Guid MetricAssetUid { get; set; }

        [DataMember]
        public DateTime? EffectiveDate { get; set; }

        [DataMember]
        public bool Result { get; set; }

        [DataMember]
        public bool IsSuccess { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }
    }

    #endregion
}
