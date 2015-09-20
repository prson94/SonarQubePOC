using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Xml.Linq;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

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

        public DateTime? UpdatedOn { get; set; }

        public int? UpdatedBy { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<Intersect> Intersects { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<IntersectTypeNode> Nodes { get; set; }

        [ForeignKey("IntersectTypeID")]
        public virtual ICollection<IntersectTypeRoleRelation> RoleRelations { get; set; }


    }
}
