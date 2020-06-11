using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using d360.core.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapTypeTemplateItem : BaseIntObject, IIntObject
    {
        [DataMember]
        public int MapTypeTemplateID { get; set; }


        [DataMember]
        public int IntersectTypeID { get; set; }


        [DataMember]
        public bool IsRequired { get; set; } = false;

    }
}
