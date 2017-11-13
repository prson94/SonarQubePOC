using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationType : BaseCreatedAndUpdatedIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [IgnoreDataMember, ForeignKey("OrganizationTypeID")]
        public virtual ICollection<Organization> Organizations { get; set; }
    }
}
