using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectTypeRole : BaseIntObject, IUpdatedMetadata
    {
        [DataMember]
        public string Name { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }

        [ForeignKey("IntersectTypeRoleID")]
        public virtual ICollection<IntersectTypeRoleRelation> RoleRelations { get; set; }
    }
}
