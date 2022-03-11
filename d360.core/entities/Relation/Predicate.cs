using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Predicate : BaseIntObject, IIntObject
    {
        [DataMember, StringLength(100)]
        public string Name { get; set; }

        [DataMember, StringLength(250)]
        public string Inverse { get; set; }

        [DataMember]
        public PredicateType Type { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid UID { get; set; }
    }

    public class PredicatesApiViewModel : List<PredicateApiViewModel>
    {
    }

    [DataContract(Namespace = NAMESPACE)]
    public class PredicateApiViewModel : BaseObject
    {
        [DataMember]
        public Guid Uid { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Inverse { get; set; }

        [DataMember]
        public PredicateType Type { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

        [DataMember]
        public bool IsInUse { get; set; }
    }
}
