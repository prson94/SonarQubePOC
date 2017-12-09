using System;

namespace igx.function.fusion.connector.eagle
{
    internal class StagingFile
    {
        public int ID { get; set; }
        public int FusionID { get; set; }
        public int FussionAttributeID { get; set; }
        public string File { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
