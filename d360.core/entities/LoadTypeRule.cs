using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadTypeRule : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int LoadTypeID { get; set; }

        [DataMember]
        public LoadTypeRuleGroup LoadTypeRuleGroup { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        [DataMember]
        public int? UniqueLoadTypeFieldID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadTypeID")]
        public virtual LoadType LoadType { get; set; }


        [IgnoreDataMember, ForeignKey("LoadTypeRuleID")]
        public virtual ICollection<LoadTypeRuleItem> LoadTypeRuleItems { get; set; }
    }
}
