using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class ObjectVersion : BaseObject
    {
        [DataMember, Key, Column(Order = 1, TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 3), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Version { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(5)]
        public string Action { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [DataMember]
        public string Value { get; set; }
    }
}
