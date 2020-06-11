using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.queue;
using System.ComponentModel.DataAnnotations.Schema;

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
