using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectMapSourceRule : BaseIntObject, IIntObject
    {
        #region Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public int IntersectMapID { get; set; }

        [DataMember]
        public int SourceRuleID { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int SortOrder { get; set; }


        #endregion

        [IgnoreDataMember]
        public IntersectMap IntersectMap { get; set; }

        [IgnoreDataMember]
        public SourceRule SourceRule { get; set; }
    }
}
