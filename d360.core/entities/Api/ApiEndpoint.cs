using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Endpoint", Schema = "api")]
    public class ApiEndpoint : BaseIntObject, IIntObject
    {
        [DataMember]
        public int ServiceID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(100)]
        public string UriPrefix { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string ItemNode { get; set; } = "item";

        [ForeignKey("ServiceID"), IgnoreDataMember]
        public virtual ApiService Service { get; set; }

        [ForeignKey("EndpointID"), IgnoreDataMember]
        public virtual ICollection<ApiEndpointVersion> Versions { get; set; }
    }
}
