using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.Models
{
    internal class AlreadyProcessedMeasureModel
    {
        public Guid MetricAssetUid { get; set; }
        public bool Deleted { get; set; }
    }
}
