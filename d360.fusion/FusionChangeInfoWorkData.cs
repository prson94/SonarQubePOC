using System.Collections.Generic;

namespace d360.fusion
{
    public class FusionChangeInfoWorkData
    {
        public FusionChangeInfoWorkData()
        {
            ChangedValues = new List<FusionFieldTempTableValue>();
        }

        public int AddCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }

        public List<FusionFieldTempTableValue> ChangedValues { get; set; }
    }
}