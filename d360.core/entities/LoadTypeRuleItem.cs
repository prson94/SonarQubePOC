using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadTypeRuleItem : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int LoadTypeRuleID { get; set; }

        [DataMember]
        public int SourceLoadTypeFieldID { get; set; }

        [DataMember]
        public string TargetFieldName { get; set; }

        [DataMember]
        public bool IsCustomField { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("SourceLoadTypeFieldID")]
        public virtual LoadTypeField LoadTypeField { get; set; }


        [IgnoreDataMember, ForeignKey("LoadTypeRuleID")]
        public virtual LoadTypeRule LoadTypeRule { get; set; }
    }
}
