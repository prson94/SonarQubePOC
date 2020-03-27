using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class DataQualityResult
    {
        public int pageSize;
        public int pageNum;
        public int total;
        public List<DataQualityResultItem> items;
    }

    public class DataQualityResultItem
    {
        public Guid ResultUid { get; set; }
        public Guid OwningAssetUid { get; set; }
        public Guid EvaluatedAssetUid { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime RunDate { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public bool Passed { get; set; }
    }    

    public class DataQualityResponseModel
    {
        public Guid Uid;
        public bool Success;
        public string Message;
    }
}
