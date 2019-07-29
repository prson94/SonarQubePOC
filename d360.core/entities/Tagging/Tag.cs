using d360.core.entities.Contracts;
using d360.core.enums;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Tag : BaseCreatedAndUpdatedIntObject
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }
        [DataMember, StringLength(250)]
        public string Value { get; set; }
        [DataMember]
        public State State { get; set; } = State.Active;

        public EventObjectInfo GetEventObjectInfo()
        {
            return new EventObjectInfo
            {
                Object = SystemObjects.Tag,
                ObjectID = Id,
                ObjectType = SystemObjects.Tag,
                ObjectTypeID = 0
            };
        }
    }
}
