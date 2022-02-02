using d360.core.entities.Contracts;
using d360.core.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities.Metric
{
    [DataContract(Namespace = NAMESPACE), Table("Allocation", Schema = "metrics")]
    public class MetricAllocation : BaseCreatedAndUpdatedGuidObject
    {
        [DataMember]
        public ScoreType ScoreType { get; set; } = ScoreType.Governance;

        [DataMember]
        public Guid AssetTypeUid { get; set; }

        [DataMember]
        public string OverrideName { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [DataMember]
        public CalculationMethod CalculationMethod { get; set; } = CalculationMethod.Weighted;

        [DataMember]
        public bool IsExternallyCalculated { get; set; }

        [DataMember]
        public int LowerThreshold { get; set; }

        [DataMember]
        public int UpperThreshold { get; set; }

        public new int CreatedBy { get; set; }
        
        public new int UpdatedBy { get; set; }
    }

    #region API Models

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

        public bool hasDisabledMeasure { get; set; }

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

    #endregion
}
