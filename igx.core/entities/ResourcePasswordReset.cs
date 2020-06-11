using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class ResourcePasswordReset : BaseGuidObject, IGuidObject
    {        
        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public DateTime CreateDate { get; set; }         
    }
}
