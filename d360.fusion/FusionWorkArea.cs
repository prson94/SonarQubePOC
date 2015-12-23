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
            FieldTempValues = new List<core.entities.Field>();            
            Relationships = new FusionRelationshipWorkData();
            Changes = new FusionChangeInfoWorkData();
            ExistingFusionAttributeDictionary = new Dictionary<string, string>();
            FusionSourceToIDMap = new Dictionary<string, int>();
        }

        public List<string> InSourceIDList { get; set; }
        
        public Dictionary<string,int> FusionSourceToIDMap { get; set; }

        public IEnumerable<core.entities.Field> FieldValueCollection { get; set; }

        public List<FusionAttributeTempTableValue> FusionAttributeTempValues { get; set; }
        
        public IEnumerable<FusionFieldIDAttributeIDMapping> FieldToAttributeMapping { get; set; }

        public Dictionary<string, string> ExistingFusionAttributeDictionary { get; set; }

        public List<core.entities.Field> FieldTempValues { get; set; }       

        public FusionRelationshipWorkData Relationships { get; set; }

        public FusionChangeInfoWorkData Changes { get; set; }
    }
}
