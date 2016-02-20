using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SourceRule : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string AppliesToObject { get; set; }

        [DataMember]
        public int AppliesToObjectID { get; set; }

        public string AppliesToObjectList { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("SourceRuleID")]
        public virtual ICollection<SourceRuleContext> Contexts { get; set; }

        [IgnoreDataMember, ForeignKey("SourceRuleID")]
        public virtual ICollection<IntersectMapSourceRule> Items { get; set; }
    }
}
