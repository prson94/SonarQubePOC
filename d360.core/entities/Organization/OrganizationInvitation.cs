using d360.core.entities.Contracts;
using System.Runtime.Serialization;

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
}
