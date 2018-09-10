using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    public class AssetCrossReference
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember]
        public string DataSource { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string ExternalID { get; set; }
        [DataMember]
        public string FieldHash { get; set; }
    }
}
