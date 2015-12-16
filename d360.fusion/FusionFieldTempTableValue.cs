using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.fusion
{
    /// <summary>
    /// Used to store values for insertion into dbo.[field] table
    /// </summary>
    public class FusionFieldTempTableValue
    {        
        public int FusionAttributeID { get; set; }
        public int FieldTypeID { get; set; }
        public string Value { get; set; }        
    }
}
