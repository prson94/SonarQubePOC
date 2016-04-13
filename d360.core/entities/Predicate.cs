using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using System.ComponentModel.DataAnnotations;

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
        public MapType Type{ get; set; }

    }
}
