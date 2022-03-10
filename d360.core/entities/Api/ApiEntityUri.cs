using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("EntityUri", Schema = "api")]
    public class ApiEntityUri : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EntityID { get; set; }

        [DataMember]
        public ApiUriType UriType { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(500)]
        public string Format { get; set; }

        [ForeignKey("EntityID"), IgnoreDataMember]
        public virtual ApiEntity Entity { get; set; }
    }
}
