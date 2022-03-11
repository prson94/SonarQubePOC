using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

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
