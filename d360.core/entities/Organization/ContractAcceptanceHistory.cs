using d360.core.entities.Contracts;
using d360.core.enums;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ContractAcceptanceHistory : BaseIntObject
    {


        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public ContractType ContractType { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [DataMember]
        public DateTime? PublishedOn { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    
    }

}
