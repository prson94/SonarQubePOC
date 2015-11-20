using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributePromotion : BaseObject
    {
        [Key, Column(Order = 1)]
        public int FusionAttributeID { get; set; }

        [Key, Column(Order = 2)]
        public string ObjectType { get; set; }

        [Key, Column(Order = 3)]
        public int ObjectID { get; set; }

        public int FusionAttributePromotionRuleID { get; set; }

        [IgnoreDataMember, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual FusionAttributePromotionRule FusionAttributePromotionRule { get; set; }
    }
}
