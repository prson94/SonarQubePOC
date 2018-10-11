using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.queue;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ShoppingCartType : BaseIntObject, IIntObject
    {
        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Name { get; set; }
    }
}
