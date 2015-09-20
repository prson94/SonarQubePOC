using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadTypeFieldDetail : BaseObject
    {
        [Key, Column(Order = 1), DataMember]
        public int ID { get; set; }

        [DataMember]
        public int LoadTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public string LookupType { get; set; }

        [DataMember]
        public string LookupName { get; set; }

        [DataMember]
        public string LookupField { get; set; }
    }
}
