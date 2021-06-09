using System;

namespace igx.jobs.scoreprocessor.Models
{
    internal class RawWorkflowScoreAsset
    {
        public Guid AssetUid { get; set; }
        public string Type { get; set; }
        public int TypeID { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
    }
}
