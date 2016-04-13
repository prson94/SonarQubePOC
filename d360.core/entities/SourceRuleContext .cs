using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class SourceRuleContext : BaseObject
    {
        #region Properties

        [Column(Order = 1), DataMember, Key]
        public int SourceRuleID { get; set; }

        [Column(Order = 2, TypeName = "varchar"), DataMember, Key, StringLength(50)]
        public string Object { get; set; }

        [Column(Order = 3), DataMember, Key]
        public int ObjectID { get; set; }

        #endregion

        [IgnoreDataMember]
        public SourceRule SourceRule { get; set; }
    }
}
