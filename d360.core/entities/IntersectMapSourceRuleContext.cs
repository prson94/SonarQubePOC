using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectMapSourceRuleContext : BaseObject
    {
        #region Properties

        [Column(Order = 1), DataMember, Key]
        public int IntersectMapSourceRuleID { get; set; }

        [Column(Order = 2), DataMember, Key]
        public string Object { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int ObjectID { get; set; }

        #endregion

        [IgnoreDataMember]
        public IntersectMapSourceRule IntersectMapSourceRule { get; set; }
    }
}
