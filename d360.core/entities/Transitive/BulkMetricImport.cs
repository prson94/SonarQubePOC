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
        public Guid MetricGroupUid { get; set; }
        [DataMember]
        public Guid MetricItemUid { get; set; }
        [DataMember]
        public DateTime? Date { get; set; }
        [DataMember]
        public bool Result { get; set; }
    }

    [DataContract(Name = "metricItemResult")]
    public class BulkMetricTemporaryTableModel
    {
        [DataMember]
        public Guid AssetUid { get; set; }
        [DataMember]
        public Guid MetricGroupUid { get; set; }
        [DataMember]
        public Guid MetricItemUid { get; set; }
        [DataMember]
        public DateTime Date { get; set; }
        [DataMember]
        public bool Result { get; set; }
        public bool IsValidAsset { get; set; }
        public bool IsValidMetricGroup { get; set; }
        public bool IsValidMetricItem { get; set; }
        [DataMember]
        public bool IsSuccess { get; set; }
        [DataMember]
        public string ErrorMessage { get; set; }
    }

    [JsonArray]
    [DataContract(Name = "metricGroups")]
    public class MetricGroupHierarchyModels : List<MetricGroupHierarchyModel>
    {

    }

    public abstract class MetricHierarchyModel : BaseObject
    {
        [DataMember]
        public decimal Weight { get; set; }
    }

    [DataContract(Name = "metricGroup")]
    public class MetricGroupHierarchyModel : MetricHierarchyModel
    {
        // These fields below just help figure the hierarchy out, and should not be sent back to client.
        public int ID { get; set; }
        public int? ParentID { get; set; }
        public int Level { get; set; }
        public string RawItems { get; set; }

        [DataMember]
        public string MetricGroupName { get; set; }
        [DataMember]
        public Guid MetricGroupUid { get; set; }
        [DataMember]
        public List<MetricGroupHierarchyModel> Groups { get; set; }
        [DataMember]
        public List<MetricItemHierarchyModel> Items { get; set; }
    }

    [DataContract(Name = "metricItem")]
    public class MetricItemHierarchyModel : MetricHierarchyModel
    {
        [DataMember]
        public string MetricItemName { get; set; }
        [DataMember]
        public Guid MetricItemUid { get; set; }
        [DataMember]
        public List<MetricItemConditionHierarchyModel> Conditions { get; set; }
    }

    [DataContract(Name = "metricItemCondition")]
    public class MetricItemConditionHierarchyModel : BaseObject
    {
        [DataMember]
        public string FieldName { get; set; }
        [DataMember]
        public string Operator { get; set; }
        [DataMember]
        public string Value { get; set; }
    }
}
