using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ContractAcceptance : BaseIntObject
    {
        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public int ContractID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }

        [DataMember]
        public DateTime? AcceptedOn { get; set; }

        [DataMember]
        public bool Accepted { get; set; }
    }

    public class ContractAcceptanceDetail : BaseIntObject
    {
        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public int ContractID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public string ResourceName { get; set; }

        [DataMember]
        public string ContractName { get; set; }

        [DataMember]
        public DateTime? AcceptedOn { get; set; }

        [DataMember]
        public bool Accepted { get; set; }
    }
}
