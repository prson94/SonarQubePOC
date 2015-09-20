using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Follow : BaseObject
    {
        [Column(Order = 1), DataMember, Key]
        public int ResourceID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public string ObjectType { get; set; }
        
        [Column(Order = 3), DataMember, Key]
        public int ObjectID { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }
    }
}
