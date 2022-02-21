using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationInvitation : BaseIntObject, IIntObject
    {
        [DataMember]
        public int OrganizationID { get; set; }

        [DataMember]
        public string Email { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationInvitationDetail : BaseIntObject
    {
        [DataMember]
        public int OrganizationID { get; set; }

        [DataMember]
        public string OrganizationName { get; set; }

        [DataMember]
        public string Email { get; set; }
    }
}
