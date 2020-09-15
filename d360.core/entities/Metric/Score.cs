using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

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

        public ICollection<ScoreItem> Items { get; set; }
    }

    #region API Models

    public class ExternalScoreResultsApiPostModel
    {

        [DataMember]
        public Guid assetUid { get; set; }

        [DataMember]
        public decimal score { get; set; }

        [DataMember]
        public DateTime? effectiveDate { get; set; }
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

    public class ExternalScoreResultsApiResultsModel
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
        public List<ExternalScoreResultMeasureModel> Measures { get { return JsonConvert.DeserializeObject<List<ExternalScoreResultMeasureModel>>((string.IsNullOrEmpty(measuresJson)) ? "[]": measuresJson); } }
        [JsonIgnore]
        public string measuresJson { get; set; }
    }

    public class ScoreResultApiPostModel
    {
        [DataMember]
        public Guid metricAssetUid { get; set; }

        [DataMember]
        public Guid assetUid { get; set; }

        [DataMember]
        public DateTime? effectiveDate { get; set; }

        [DataMember]
        public bool result { get; set; }
    }

    #endregion
}
