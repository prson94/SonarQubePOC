using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationType : BaseCreatedAndUpdatedIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string DisplayFormat { get; set; }

        [DataMember]
        public State State { get; set; } = State.Active;

        [IgnoreDataMember, ForeignKey("OrganizationTypeID")]
        public virtual ICollection<Organization> Organizations { get; set; }
    }

    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationTypeDetail : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int AssetTypeID { get; set; }

        [DataMember]
        public string OrganizationCount { get; set; }

        [DataMember]
        public Guid uid { get; set; }
    }
}
