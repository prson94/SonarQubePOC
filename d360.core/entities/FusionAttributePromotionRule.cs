using System;
using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributePromotionRule : BaseIntObject, IUpdatedMetadata
    {
        public int FusionID { get; set; }

        public bool Enabled { get; set; }

        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        public string PromotionObjectType { get; set; }

        public int PromotionObjectID { get; set; }

        public string PromotionParentObjectType { get; set; }

        public int? PromotionParentObjectID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }

        [IgnoreDataMember]//, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual ICollection<FusionAttributePromotionRuleItem> FusionAttributePromotionRuleItems { get; set; }

        [IgnoreDataMember]//, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual ICollection<FusionAttributePromotionRuleMapping> FusionAttributePromotionRuleMappings { get; set; }
    }
}
