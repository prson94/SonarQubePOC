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
            Relationships = new FusionRelationshipWorkData();
            Changes = new FusionChangeInfoWorkData();
            ExistingFusionAttributeDictionary = new SortedList<string, string>();
        }

        public List<string> InSourceIDList { get; set; }

        public IEnumerable<FusionAttributeToParentMapping> AttributeMappingCollection { get; set; }

        public IEnumerable<core.entities.Field> FieldValueCollection { get; set; }

        public List<FusionAttributeTempTableValue> FusionAttributeTempValues { get; set; }
        
        public IEnumerable<FusionFieldIDAttributeIDMapping> FieldToAttributeMapping { get; set; }

        public SortedList<string, string> ExistingFusionAttributeDictionary { get; set; }

        public List<FusionFieldTempTableValue> FieldTempValues { get; set; }       

        public FusionRelationshipWorkData Relationships { get; set; }

        public FusionChangeInfoWorkData Changes { get; set; }
    }
}
