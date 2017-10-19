using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapTypeTemplate : BaseIntObject, IIntObject
    {

        [DataMember]
        public int MapTypeID { get; set; }

        [DataMember]
        public string Name { get; set; }

    }
}
