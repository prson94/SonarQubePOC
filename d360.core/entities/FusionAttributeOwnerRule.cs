using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributeOwnerRule : BaseIntObject, IUpdatedMetadata
    {
        public int FusionID { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string RelationshipOwnerObjectType { get; set; }

        public int RelationshipOwnerObjectID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<FusionAttributeOwnerRuleItem> FusionAttributeOwnerRuleItems { get; set; }
    }
}
