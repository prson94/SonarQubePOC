using System;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationRegistration : BaseGuidObject
    {
        [DataMember]
        public int OrganizationID { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public RegisterStep Step { get; set; }

        [DataMember]
        public DateTime RegisteredStartedOn { get; set; }

        [DataMember]
        public DateTime? RegisteredCompletedOn { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    }
}
