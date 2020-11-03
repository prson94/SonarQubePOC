using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.Models
{
    internal class MeasureRemovedScoreRequeueDataItem
    {
        public Guid AssetUid { get; set; }
        public Guid MetricAssetUid { get; set; }
        public Guid MetricAssetVersionUid { get; set; }
        public bool Result { get; set; }
    }
}
