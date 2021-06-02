using d360.core.entities;
using System.Collections.Generic;

namespace igx.jobs.scoreprocessor.Models
{
    internal class WorkflowScoreGroup
    {
        public string Type { get; set; }
        public int TypeID { get; set; }
        public List<WorkflowScoredAsset> Assets { get; set; }
    }
}
