using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class OrganizationResource : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int OrganizationID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ResourceID { get; set; }

        [DataMember]
        public bool? Accepted { get; set; }

        [DataMember]
        public DateTime? DateAccepted { get; set; }


        [DataMember, ForeignKey("ResourceID")]
        public GlobalReportingResource Resource { get; set; }

        [IgnoreDataMember]
        public virtual Organization Organization { get; set; }
    }
}
