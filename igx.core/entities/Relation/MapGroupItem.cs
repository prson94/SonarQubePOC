using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapGroupItem : BaseIntObject, IIntObject
    {
        [DataMember]
        public int MapGroupID { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int  ObjectID { get; set; }

    }
}
