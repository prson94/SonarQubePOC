using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("LeafAttributes", Schema = "fusion")]
    public class LeafFusionAttribute : BaseObject
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public string AttributeName { get; set; }

        [DataMember]
        public string AttributePath { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string TypePath { get; set; }

        [DataMember]
        public string Tab { get; set; }

        [DataMember]
        public string FusionName { get; set; }

        [DataMember]
        public string Url { get; set; }
    }
}
