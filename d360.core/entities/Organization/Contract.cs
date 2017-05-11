using d360.core.entities.Contracts;
using d360.core.enums;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Contract : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public ContractType ContractType { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ContractModel : BaseIntObject
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public string OrganizationName { get; set; }

        [DataMember]
        public ContractType ContractType { get; set; }

        [DataMember]
        public string ContractTypeName { get; set; }

        [DataMember]
        public string ContractTypeDescription { get; set; }
    }
}
