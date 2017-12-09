using System.Collections.Generic;

namespace igx.function.fusion.load
{
    internal class FusionWorkArea
    {
        public FusionWorkArea()
        {
            InSourceIDList = new List<string>();
            FusionAttributeTempValues = new List<FusionAttributeTempTableValue>();
            FieldTempValues = new List<d360.core.entities.Field>();            
            Relationships = new FusionRelationshipWorkData();
            Changes = new FusionChangeInfoWorkData();
            ExistingFusionAttributes = new Dictionary<string, FusionAttributeTempTableValue>();
            FusionSourceToIDMap = new Dictionary<string, int>();
        }

        public List<string> InSourceIDList { get; set; }
        
        public Dictionary<string,int> FusionSourceToIDMap { get; set; }

        public IEnumerable<d360.core.entities.Field> FieldValueCollection { get; set; }

        public List<FusionAttributeTempTableValue> FusionAttributeTempValues { get; set; }
        
        public IEnumerable<FusionFieldIDAttributeIDMapping> FieldToAttributeMapping { get; set; }
               
        public Dictionary<string, FusionAttributeTempTableValue> ExistingFusionAttributes { get; set; }

        public List<d360.core.entities.Field> FieldTempValues { get; set; }       

        public FusionRelationshipWorkData Relationships { get; set; }

        public FusionChangeInfoWorkData Changes { get; set; }
    }
}
