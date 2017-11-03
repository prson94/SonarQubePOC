using d360.core.entities.Contracts;
using d360.core.enums;
using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Client : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public ClientState Status { get; set; }

        [DataMember]
        public Guid PublicID { get; set; }

        //[IgnoreDataMember, ForeignKey("ClientID")]
        //public virtual ICollection<Company> Companies { get; set; }
    }
}
