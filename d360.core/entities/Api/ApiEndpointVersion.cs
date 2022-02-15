using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("EndpointVersion", Schema = "api")]
    public class ApiEndpointVersion : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EndpointID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(100)]
        public string UriPrefix { get; set; }

        [DataMember]
        public int MajorVersion { get; set; }

        [DataMember]
        public int MinorVersion { get; set; }

        [ForeignKey("EndpointID"), IgnoreDataMember]
        public virtual ApiEndpoint Endpoint { get; set; }

        [ForeignKey("EndpointVersionID"), IgnoreDataMember]
        public virtual ICollection<ApiEntity> Entities { get; set; }
    }
}
