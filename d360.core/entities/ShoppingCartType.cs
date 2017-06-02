using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ShoppingCartType : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }
    }
}
