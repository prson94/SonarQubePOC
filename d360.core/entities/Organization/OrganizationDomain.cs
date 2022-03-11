using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationDomain : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public int OrganizationID { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    }
}
