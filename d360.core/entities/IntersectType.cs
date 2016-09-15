using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.IntersectType, "IntersectType")]
    public class IntersectType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        [ReadOnly(true)]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Name { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Subject { get; set; }

        [DataMember]
        public int SubjectID { get; set; }

        [DataMember, Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public bool? IsSystem { get; set; }

        [DataMember]
        public int? PredicateID { get; set; }

        [ForeignKey("PredicateID")]
        public virtual Predicate Predicate { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<Intersect> Intersects { get; set; }

        //[ForeignKey("IntersectTypeID")]
        //public virtual ICollection<IntersectTypeNode> Nodes { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<IntersectTypePredicate> IntersectTypePredicates { get; set; }
    }
}
