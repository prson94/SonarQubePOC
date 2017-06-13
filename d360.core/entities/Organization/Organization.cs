using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Organization : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool? Accepted { get; set; }

        [DataMember]
        public int? AcceptedBy { get; set; }

        [DataMember]
        public DateTime? DateAccepted { get; set; }

        [DataMember]
        public string AdministratorEmail { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<Contract> Contracts { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationDomain> OrganizationDomains { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationInvitation> OrganizationInvitations { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationResource> OrganizationResources { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationDetail : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool? Accepted { get; set; }

        [DataMember]
        public int? AcceptedBy { get; set; }

        [DataMember]
        public DateTime? DateAccepted { get; set; }

        [DataMember]
        public string AdministratorEmail { get; set; }

        [DataMember]
        public string AcceptedByName { get; set; }
    }
}
