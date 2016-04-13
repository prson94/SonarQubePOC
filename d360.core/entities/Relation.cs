using d360.core.entities.Contracts;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class Relation : BaseIntObject, IUpdatedMetadata, ICreatedObject, IUpdatedObject, IIntObject
    {
        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int RelationTypeID { get; set; }

        [DataMember]
        public int PredicateID { get; set; }

        [DataMember]
        public bool Deleted { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [IgnoreDataMember]
        public virtual RelationType RelationType { get; set; }
    }
}
