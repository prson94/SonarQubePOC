using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Scoring
{
    public class AllocationApiGetModel
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetTypeClass assetClassName { get; set; } = AssetTypeClass.BusinessAsset;
        [DataMember]
        public Guid assetTypeUid { get; set; }
        [DataMember]
        public string assetTypePath { get; set; }
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScoreType scoreType { get; set; } = ScoreType.Governance;
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public State state { get; set; } = State.Active;
        public bool hasMeasure { get; set; }
        public bool hasField { get; set; }
        public bool isExternallyCalculated { get; set; }
        public int lowerThreshold { get; set; }
        public int upperThreshold { get; set; }
    }

    public class AllocationApiUpsertModel
    {
        [DataMember]
        public Guid assetTypeUid { get; set; }

        [DataMember]
        public ScoreType scoreType { get; set; }

        [DataMember]
        public bool isExternallyCalculated { get; set; }
        [DataMember]
        public int? lowerThreshold { get; set; }
        [DataMember]
        public int? upperThreshold { get; set; }
    }

    public class AllocationApiGetUnallocatedAssetTypeModel
    {
        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetTypeClass assetTypeClass { get; set; }

        [DataMember]
        public Guid? assetTypeUid { get; set; }

        [DataMember]
        public string assetTypePath { get; set; }
    }

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
        public Guid AssetUid { get; set; }
        public decimal Score { get; set; }
        public DateTime RunDate { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public List<ExternalScoreResultMeasureModel> Measures { get; set; }
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
        public bool result{ get; set; }
    }
}
