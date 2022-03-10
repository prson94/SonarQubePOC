using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class CompanyResource : BaseObject
    {
        [Column(Order = 1), Key]
        public int CompanyID { get; set; }

        [Column(Order = 2), Key]
        public int ResourceID { get; set; }

        public bool IsAdministrator { get; set; }

        [DataMember]
        public DateTime? LastLoggedInOn { get; set; }

        [DataMember]
        public CompanyResourceState State { get; set; }

        public Resource Resource { get; set; }
    }
}
