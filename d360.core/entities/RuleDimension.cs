using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{    
    [DataContract(Namespace = NAMESPACE)]
    public class RuleDimension : BaseIntObject, IIntObject
    {        
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }
                
        public DateTime UpdatedOn { get; set; }
                
        public int? UpdatedBy { get; set; }
    }
}
