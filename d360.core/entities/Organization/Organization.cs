using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Organization : BaseIntObject, IIntObject, IFieldsObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int OrganizationTypeID { get; set; }

        [DataMember]
        public bool? Accepted { get; set; }

        [DataMember]
        public int? AcceptedBy { get; set; }

        [DataMember]
        public DateTime? DateAccepted { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string AdministratorEmail { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [IgnoreDataMember, ForeignKey("OrganizationTypeID")]
        public virtual OrganizationType OrganizationType { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<Contract> Contracts { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationDomain> OrganizationDomains { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationInvitation> OrganizationInvitations { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationID")]
        public virtual ICollection<OrganizationResource> OrganizationResources { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel
            {
                Object = SystemObjects.Organization,
                Type = SystemObjects.OrganizationType,
                TypeID = OrganizationTypeID
            };
        }
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

        [DataMember]
        public int OrganizationTypeID { get; set; }
    }
}
