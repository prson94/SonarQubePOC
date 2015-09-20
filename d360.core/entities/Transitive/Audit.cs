using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract]
    public partial class Audit
    {
        [DataMember]
        public string FieldName { get; set; }

        [DataMember]
        public string FieldValue { get; set; }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public int CreatingResourceID { get; set; }

        [DataMember]
        public string CreatingResourceName { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }
    }
}
