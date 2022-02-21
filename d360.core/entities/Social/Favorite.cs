using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Favorite : BaseIntObject, IIntObject
    {
        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Route { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int? ObjectID { get; set; }

        [DataMember]
        public bool IsHomePage { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Type { get; set; }

        [DataMember]
        public Guid? Uid { get; set; }
    }
}
