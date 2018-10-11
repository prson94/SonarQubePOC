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
    public class Language : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        [Column(TypeName = "varchar"), StringLength(2)]
        public string Alpha2 { get; set; }
        [DataMember]
        [Column(TypeName = "varchar"), StringLength(3)]
        public string Alpha3b { get; set; }
    }
}
