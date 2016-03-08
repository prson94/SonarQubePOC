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
    public class IntersectMapSourceRule : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public int IntersectMapID { get; set; }

        [DataMember]
        public int SourceRuleID { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int SortOrder { get; set; }
        #endregion

        #region Not Mapped
        [DataMember, NotMapped]
        public int ObjectID { get; set; }
        
        [DataMember, NotMapped]
        public string Object { get; set; }

        [DataMember, NotMapped]
        public string Name { get; set; }

        [DataMember, NotMapped]
        public string IconForeColor { get; set; }

        [DataMember, NotMapped]
        public string IconBackColor { get; set; }
        #endregion

        [IgnoreDataMember]
        public IntersectMap IntersectMap { get; set; }

        [IgnoreDataMember]
        public SourceRule SourceRule { get; set; }

        [DataMember, ForeignKey("IntersectMapSourceRuleID")]
        public virtual ICollection<IntersectMapSourceRuleContext> Contexts { get; set; }
    }
}
