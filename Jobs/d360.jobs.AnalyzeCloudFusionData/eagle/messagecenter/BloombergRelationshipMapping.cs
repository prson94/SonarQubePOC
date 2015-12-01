using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace d360.jobs.AnalyzeCloudFusionData.eagle.messageCenter
{    
    public class BloombergRelationshipMapping : IRelationshipMapping
    {        

        /// <summary>
        /// The contents of the tag attribute 
        /// possible values are a constant or a star tag (unsigned int)
        /// </summary>
        public string Tag { get; set;}

        public string Expression { get; set; }

        public string UserDescription{ get; set; }        
        public string ConstantName { get; set; }
        public string ConstantValue { get; set; }
        public uint StarTag { get; set; }        
        public RelationshipColumnType ColumnTagType { get; set; }
        public RelationshipExpressionType ExpressionValueType { get; set; }

        List<string> _bbMnemonics = new List<string>();
        public List<string> BloombergMnemonics { get { return _bbMnemonics; } }
        
    }
}
