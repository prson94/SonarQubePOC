using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypePredicate : BaseIntObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int PredicateID { get; set; }
    }
}
