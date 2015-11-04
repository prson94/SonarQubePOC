using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.Intersect, "Intersect")]
    public class Intersect : BaseIntObject, IIntObject
    {
        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int? IntersectTypeRoleID { get; set; }

        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Name { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IntersectClassification_Name", Description = "IntersectClassification_Description")]
        public IntersectClassification? Classification { get; set; }

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "IntersectDescription_Name", Description = "IntersectDescription_Description")]
        public string Description { get; set; }

        [IgnoreDataMember]
        public virtual IntersectType IntersectType { get; set; }

        [IgnoreDataMember]
        public virtual IntersectTypeRole IntersectTypeRole { get; set; }

        [IgnoreDataMember, ForeignKey("IntersectID")]
        public virtual ICollection<IntersectNode> Nodes { get; set; }
    }
}
