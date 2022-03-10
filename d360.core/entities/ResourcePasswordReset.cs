using System;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

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
