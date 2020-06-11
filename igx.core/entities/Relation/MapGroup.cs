using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapGroup : BaseIntObject, IIntObject, ICreatedObject
    {
        [DataMember]
        public string BusinessTransformation { get; set; }

        [DataMember]
        public string TechnicalTransformation { get; set; }
    }
}
