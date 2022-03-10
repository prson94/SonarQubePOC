using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.queue;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ShoppingCart : BaseIntObject, IIntObject, IEventTrackedEntity
    {
        [DataMember]
        public int ShoppingCartTypeID { get; set; }

        [DataMember]
        public int ResourceID { get; set; }
        
        [DataMember]
        public DateTime CreatedOn { get; set; }
        
        [DataMember]
        public DateTime? RequestedOn { get; set; }

        [DataMember]
        public string Request { get; set; }

        [IgnoreDataMember]
        public ShoppingCartType ShoppingCartType { get; set; }

        [DataMember, NotMapped]
        public string Requestor { get; set; }

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.ShoppingCart,
                ObjectID = ID,
                ObjectType = SystemObjects.ShoppingCartType,
                ObjectTypeID = ShoppingCartTypeID
            };
        }
    }
}
