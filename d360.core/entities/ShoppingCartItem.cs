using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ShoppingCartItem : BaseObject
    {
        [DataMember, Key, Column(Order = 1)]
        public int ShoppingCartID { get; set; }
        
        [DataMember, Key, Column(Order = 2, TypeName = "varchar"), StringLength(250)]
        public string Object { get; set; }
        
        [DataMember, Key, Column(Order = 3)]
        public int ObjectID { get; set; }
        
        [DataMember]
        public DateTime AddedOn { get; set; }

        [DataMember, NotMapped]
        public string Url { get; set; }

        [IgnoreDataMember]
        public ShoppingCart ShoppingCart { get; set; }
    }
}
