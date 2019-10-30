using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Metric
{
    public class MetricScoreApiModel
    {
        public int pageSize { get; set; } = 250;
        public int pageNum { get; set; } = 1;
        public int total { get; set; }
        public List<MetricAssetScoreModel> items { get; set; } = new List<MetricAssetScoreModel>();
    }

    public class MetricAssetScoreModel
    {
        public Guid AssetUid { get; set; }
        public List<MetricScoreModel> Scores { get; set; } = new List<MetricScoreModel>();
    }
    public class MetricScoreModel
    {
        public DateTime EffectiveDate { get; set; }
        public float Score { get; set; }
    }

}
