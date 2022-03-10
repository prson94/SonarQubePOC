using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;


namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Contract : BaseCreatedAndUpdatedIntObject, IIntObject, IUpdatedObject, IUpdatedMetadata, ICreatedObject, ICreatedMetadata
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Body { get; set; }

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

    [DataContract(Namespace = NAMESPACE)]
    public class ContractDetail : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Title { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public ContractType ContractType { get; set; }

        [DataMember]
        public string OrganizationName { get; set; }

        [DataMember]
        public string ContractTypeName { get; set; }

        [DataMember]
        public string ContractTypeDescription { get; set; }

        [DataMember]
        public DateTime? PublishedOn { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class ContractValidation : BaseObject
    {
        [DataMember]
        public int ContractID { get; set; }

        [DataMember]
        public int? OrganizationID { get; set; }

        [DataMember]
        public ContractType ContractType { get; set; }

        [DataMember]
        public bool Accepted { get; set; }

        [DataMember]
        public bool IsFirstUser { get; set; }
    }

    public static class ContractValidationCacheModel
    {

        public const string cacheKey = "ContractValidations";
        public const int cacheDuration = 15;

        public class User
        {
            public int ID { get; set; }
            public ConcurrentBag<Company> Companies = new ConcurrentBag<Company>();
        }

        public class Company
        {
            public int ID { get; set; }
            public bool ContractsAccepted { get; set; }
        }
    }
}
