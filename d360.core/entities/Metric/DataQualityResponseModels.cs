using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    [DataContract]
    public class DataQualityGetResultModel
    {
        [DataMember]
        public int pageSize { get; set; }
        [DataMember]
        public int pageNum { get; set; }
        [DataMember]
        public int total { get; set; }
        [DataMember]
        public List<DataQualityGetResultItem> items { get; set; }
    }
    [DataContract]
    public class DataQualityGetResultItem
    {
        [DataMember]
        public Guid ResultUid { get; set; }
        [DataMember]
        public Guid OwningAssetUid { get; set; }
        [DataMember]
        public Guid? EvaluatedAssetUid { get; set; }
        [DataMember]
        public string EvaluatedAssetPath { get; set; }
        [DataMember]
        public string EvaluatedAssetTypePath { get; set; }               
        public string EvaluatedAssetSegments { get; set; }
        [DataMember]
        public string EvaluatedAssetDisplayPath { get; set; }
        [IgnoreDataMember]
        public AssetTypeClass? EvaluatedAssetTypeClass { get; set; }
        [DataMember]
        public string EvaluatedAssetClass { get { return EvaluatedAssetTypeClass.HasValue ? EvaluatedAssetTypeClass.Value.GetDisplayName() : null; } }
        [DataMember]
        public DateTime EffectiveDate { get; set; }
        [DataMember]
        public DateTime RunDate { get; set; }
        [DataMember]
        public long TotalCount { get; set; }
        [DataMember]
        public long PassCount { get; set; }
        [DataMember]
        public long FailCount { get; set; }
        [DataMember]
        public double? PassFraction { get; set; }
        [DataMember]
        public bool? Passed { get; set; }
        [DataMember]
        public bool? IsDuplicate { get; set; }
    }

    public class DataQualityResponseModel
    {
        public int ItemNumber { get; set; }
        public Guid? Uid { get; set; }
        public Guid ExecutionItemUid { get; set; }
        public bool Success { get; set; } = false;
        public string Message { get; set; }
    }

    public class DataQualityDeleteResponseModel
    {
        public Guid ExecutionItemUid { get; set; }
        public bool Success { get; set; } = false;
        public string Message { get; set; }
    }

    public class ExecutionDeletedAssetResult 
    {
        public Guid ExecutionID { get; set; }
        public int ItemNumber { get; set; }
        public Guid Uid { get; set; }
        public Guid ExecutionItemUid { get; set; }
        public Guid OwningAssetUid { get; set; }
        public Guid EvaluatedAssetUid { get; set; }
        public DateTime? EffectiveDateStart { get; set; }
        public DateTime? EffectiveDateEnd { get; set; }
        public DateTime? RunDateStart { get; set; }
        public DateTime? RunDateEnd { get; set; }
        public string Message { get; set; }
        public bool? Success { get; set; }
    }
}
