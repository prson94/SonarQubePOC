using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Namespace", Schema = "api")]
    public class ApiNamespace : BaseIntObject, IIntObject
    {
        [DataMember]
        public int ServiceID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Node { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Namespace { get; set; }

        [ForeignKey("ServiceID"), IgnoreDataMember]
        public virtual ApiService Service { get; set; }
    }
}
