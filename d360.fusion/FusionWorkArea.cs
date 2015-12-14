using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.fusion
{
    internal class FusionWorkArea
    {
        public FusionWorkArea()
        {
            InSourceIDList = new List<string>();
            FusionAttributeTempValues = new List<FusionAttributeTempTableValue>();
            FieldTempValues = new List<FusionFieldTempTableValue>();
            ChangedValues = new List<FusionFieldTempTableValue>();
        }

        public List<string> InSourceIDList { get; set; }

        public IEnumerable<FusionAttributeToParentMapping> AttributeMappingCollection { get; set; }

        public IEnumerable<FusionFieldValues> FieldValueCollection { get; set; }

        public List<FusionAttributeTempTableValue> FusionAttributeTempValues { get; set; }
        

        public IEnumerable<FusionFieldIDAttributeIDMapping> FieldToAttributeMapping { get; set; }

        public List<FusionFieldTempTableValue> FieldTempValues { get; set; }

        public int AddCount { get; set; }
        public int UpdateCount { get; set; }
        public int DeleteCount { get; set; }

        public List<FusionFieldTempTableValue> ChangedValues { get; set; }
    }
}
