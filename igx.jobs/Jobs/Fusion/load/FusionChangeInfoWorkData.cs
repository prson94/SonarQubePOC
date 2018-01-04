using System.Collections.Generic;

namespace igx.jobs.fusion.load
{
    public class FusionChangeInfoWorkData
    {
        public FusionChangeInfoWorkData()
        {
            ChangedValues = new List<FusionChangeTableValue>();
        }

        public int AddCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }

        
        public List<FusionChangeTableValue> ChangedValues { get; set; }
    }
}