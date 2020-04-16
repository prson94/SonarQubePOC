using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class DataQualityResult
    {
        public int pageSize { get; set; }
        public int pageNum { get; set; }
        public int total { get; set; }
        public List<DataQualityResultItem> items { get; set; }
    }

    public class DataQualityResultItem
    {
        public Guid ResultUid { get; set; }
        public Guid OwningAssetUid { get; set; }
        public Guid EvaluatedAssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime RunDate { get; set; }
        public long PassCount { get; set; }
        public long FailCount { get; set; }
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
