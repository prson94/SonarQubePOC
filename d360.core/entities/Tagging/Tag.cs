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
    public class Tag : BaseCreatedAndUpdatedIntObject
    {
        [DataMember]
        public Guid uid { get; set; }
        [DataMember, StringLength(250)]
        public string Value { get; set; }
        
    }
}
