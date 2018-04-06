using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Service", Schema = "api")]
    public class ApiService : BaseIntObject, IIntObject
    {
        [DataMember]
        public string UriPrefix { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }


        [DataMember]
        public int MaximumCacheAge { get; set; }

        [ForeignKey("ServiceID"), IgnoreDataMember]
        public virtual ICollection<ApiEndpoint> Endpoints { get; set; }
    }
}
