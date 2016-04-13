using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributePromotionRule : BaseIntObject, IUpdatedMetadata
    {
        public int FusionID { get; set; }

        public bool Enabled { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string PromotionObjectType { get; set; }

        public int PromotionObjectID { get; set; }

        [Column(TypeName = "varchar"), StringLength(25)]
        public string PromotionParentObjectType { get; set; }

        public int? PromotionParentObjectID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual ICollection<FusionAttributePromotion> FusionAttributePromotions { get; set; }

        [IgnoreDataMember, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual ICollection<FusionAttributePromotionRuleItem> FusionAttributePromotionRuleItems { get; set; }

        [IgnoreDataMember, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual ICollection<FusionAttributePromotionRuleMapping> FusionAttributePromotionRuleMappings { get; set; }
    }
}
