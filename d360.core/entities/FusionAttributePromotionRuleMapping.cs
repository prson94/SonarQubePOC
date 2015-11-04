using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributePromotionRuleMapping : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int FusionAttributePromotionRuleID { get; set; }

        [DataMember]
        public string SourceFieldName { get; set; }

        [DataMember]
        public int SourceFieldTypeID { get; set; }

        [DataMember]
        public string TargetFieldName { get; set; }

        [DataMember]
        public int TargetFieldTypeID { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("FusionAttributePromotionRuleID")]
        public virtual FusionAttributePromotionRule FusionAttributePromotionRule { get; set; }
    }
}
