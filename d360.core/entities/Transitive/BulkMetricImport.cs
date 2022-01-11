using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities
{

    #region Used in Metrics API to display the metric results by asset.

    [DataContract]
    public class MetricAssetHierarchyConditionsModel
    {
        [DataMember]
        public Guid Uid { get; set; }
        [DataMember]
        public string Weight { get; set; }
        [DataMember]
        public string MatchType { get; set; }
        
        [DataMember]
        public string Position { get; set; }
        
        [DataMember]
        public List<MetricAssetHierarchyConditionModel> ConditionItems { get; set; }
    }

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
        public Guid ScoreItemUid { get; set; }

        [DataMember]
        public bool IsGroup { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public float? Threshold { get; set; }

        [DataMember]
        public decimal Weight { get; set; }

        [DataMember]
        public decimal AdjustedWeight { get; set; }

        [DataMember]
        public decimal AdjustedMaxWeight { get; set; }

        [DataMember]
        public decimal DisplayWeight { get; set; }

        [DataMember]
        public decimal DisplayMaxWeight { get; set; }

        [DataMember]
        public bool? Value { get; set; }

        [DataMember]
        public float? DecimalValue { get; set; }

        [DataMember]
        public ScoreType ScoreType { get; set; }

        [DataMember]
        public DateTime? EffectiveDate { get; set; }

        [DataMember]
        public DateTime? RunDate { get; set; }

        [DataMember]
        public DateTime? EndDate { get; set; }
        [DataMember]
        public bool? MatchConditionsOnly { get; set; }
        [DataMember]
        public Guid? ConditionUid { get; set; }

    }

    [DataContract(Name = "metric")]
    public class ChildMetricAssetHierarchyModel : BaseMetricAssetHierarchyModel
    {
        [DataMember]
        public List<MetricAssetHierarchyConditionsModel> Conditions { get; set; }
    }

    [DataContract(Name = "metric")]
    public class RootMetricAssetHierarchyModel : BaseMetricAssetHierarchyModel
    {
        public string ConditionsJson { get; set; }

        public string MeasuresJson { get; set; }
        
        public string OtherConditionsJSON { get; set; }

        [DataMember]
        public List<MetricAssetHierarchyConditionsModel> Conditions { get { return string.IsNullOrEmpty(ConditionsJson) ? null : JsonConvert.DeserializeObject<List<MetricAssetHierarchyConditionsModel>>(ConditionsJson ?? "[]"); } }

        [DataMember]
        public List<ChildMetricAssetHierarchyModel> Measures { get { return string.IsNullOrEmpty(MeasuresJson) ? null : JsonConvert.DeserializeObject<List<ChildMetricAssetHierarchyModel>>(MeasuresJson ?? "[]"); } }
        
        [DataMember]
        public List<string> OtherConditions { get { return string.IsNullOrEmpty(OtherConditionsJSON) ? null : JsonConvert.DeserializeObject<List<string>>(OtherConditionsJSON ?? "[]"); } }
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
