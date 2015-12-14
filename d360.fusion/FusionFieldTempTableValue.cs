using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.fusion
{
    public class FusionFieldTempTableValue
    {
        //public string SourceID { get; set; }        
        public int FusionAttributeID { get; set; }
        public int FieldTypeID { get; set; }
        public string Value { get; set; }
        public string OldValue { get; set;}
        public string Action { get; set; }
    }
}
