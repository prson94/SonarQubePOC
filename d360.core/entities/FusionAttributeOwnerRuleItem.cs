using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FusionAttributeOwnerRuleItem : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int FusionAttributeOwnerRuleID { get; set; }

        [DataMember]
        public int? FusionAttributeID { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("FusionAttributeOwnerRuleID")]
        public virtual FusionAttributeOwnerRule FusionAttributeOwnerRule { get; set; }
    }
}
