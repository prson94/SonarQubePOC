using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class DataQualityGetResultModel
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int total { get; set; }
        public List<DataQualityGetResultItem> items { get; set; }
    }

    public class DataQualityGetResultItem
    {
        public Guid ResultUid { get; set; }
        public Guid OwningAssetUid { get; set; }
        public Guid? EvaluatedAssetUid { get; set; }
        public string EvaluatedAssetPath { get; set; }
        public string EvaluatedAssetTypePath { get; set; }
        public string EvaluatedAssetClass { get; set; }        
        public DateTime EffectiveDate { get; set; }
        public DateTime RunDate { get; set; }
        public long TotalCount { get; set; }
        public long PassCount { get; set; }
        public long FailCount { get; set; }
        public double PassFraction { get; set; }
        public bool Passed { get; set; }
    }    

    public class DataQualityResponseModel
    {

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
}
