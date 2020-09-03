using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [JsonArray]
    [DataContract(Name = "metrics")]
    public class BulkMetricsImport : List<BulkMetricImport>
    {

    }

    [DataContract(Name = "metric")]
    public class BulkMetricImport : BaseObject
    {
        [DataMember]
        public Guid AssetUid { get; set; }
        [DataMember]
        public Guid MetricAssetUid { get; set; }
        [DataMember]
        public DateTime? EffectiveDate { get; set; }
        [DataMember]
        public bool Result { get; set; }
    }

    [DataContract(Name = "metricItemResult")]
    public class BulkMetricTemporaryTableModel
    {
        [DataMember]
        public Guid AssetUid { get; set; }
        [DataMember]
        public Guid MetricAssetUid { get; set; }
        [DataMember]
        public DateTime EffectiveDate { get; set; }
        [DataMember]
        public bool Result { get; set; }
        [DataMember]
        public bool IsSuccess { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
    }

    #region Used in Metrics API to display the metric results by asset.

    [DataContract]
    public class MetricAssetHierarchyConditionModel 
    {
        [DataMember]
        public string FieldName { get; set; }
        
        [DataMember]
        public string Operator { get; set; }
        
        [DataMember]
        public string Value { get; set; }
    }

    public class BaseMetricAssetHierarchyModel : BaseObject
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public Guid? ParentUid { get; set; }

        [DataMember]
        public bool IsGroup { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public decimal AdjustedWeight { get; set; }

        [DataMember]
        public decimal AdjustedMaxWeight { get; set; }

        [DataMember]
        public bool Value { get; set; }

        [DataMember]
        public ScoreType ScoreType { get; set; }

        [DataMember]
        public DateTime? EffectiveDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }


    }

    [DataContract(Name = "metric")]
    public class ChildMetricAssetHierarchyModel : BaseMetricAssetHierarchyModel
    {
        [DataMember]
        public List<MetricAssetHierarchyConditionModel> Conditions { get; set; }
    }

    [DataContract(Name = "metric")]
    public class RootMetricAssetHierarchyModel : BaseMetricAssetHierarchyModel
    {
        public string ConditionsJson { get; set; }

        public string MeasuresJson { get; set; }

        [DataMember]
        public List<MetricAssetHierarchyConditionModel> Conditions { get { return string.IsNullOrEmpty(ConditionsJson) ? null : JsonConvert.DeserializeObject<List<MetricAssetHierarchyConditionModel>>(ConditionsJson ?? "[]"); } }

        [DataMember]
        public List<ChildMetricAssetHierarchyModel> Measures { get { return string.IsNullOrEmpty(MeasuresJson) ? null : JsonConvert.DeserializeObject<List<ChildMetricAssetHierarchyModel>>(MeasuresJson ?? "[]"); } }
    }

    #endregion

    #region Used in Metrics API to define the metrics when calling the definition by asset type.

    [JsonArray, DataContract(Name = "metrics")]
    public class MetricAssetTypeHierarchyModels : List<MetricAssetTypeHierarchyModel>
    {

    }

    [DataContract(Name = "metric")]
    public class MetricAssetTypeHierarchyModel : BaseObject
    {
        [DataMember]
        public Guid Uid { get; set; }
        public Guid? ParentUid { get; set; }
        public int Level { get; set; }
        [DataMember]
        public bool IsGroup { get; set; }
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public DateTime? EffectiveDate { get; set; }

        public string ConditionsJson { get; set; }


        [DataMember]
        public List<MetricAssetTypeHierarchyModel> Metrics { get; set; }

        [DataMember]
        public List<MetricConditionHierarchyModel> Conditions { get; set; }
    }

    [DataContract(Name = "metricItemCondition")]
    public class MetricConditionHierarchyModel : BaseObject
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string Operator { get; set; }

        //public string ValueJson { get; set; }

        // Future use (mpappas) for when we start adding potentially multiple values that the JSON property above could store.
        [DataMember]
        public string Value { get; set; }

        //[DataMember]
        //public List<string> Values { get; set; }
    }

    #endregion
}
