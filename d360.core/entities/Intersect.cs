using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Intersect, "Intersect")]
    public class Intersect : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IntersectClassification_Name", Description = "IntersectClassification_Description")]
        public IntersectClassification? Classification { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IntersectDescription_Name", Description = "IntersectDescription_Description")]
        public string Description { get; set; }

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

        [IgnoreDataMember]
        public virtual IntersectType IntersectType { get; set; }

        [IgnoreDataMember, ForeignKey("IntersectID")]
        public virtual ICollection<IntersectNode> Nodes { get; set; }
    }
}
