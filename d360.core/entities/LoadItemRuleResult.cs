using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class LoadItemRuleResult : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order=1)]
        public int LoadItemID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int LoadTypeRuleID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string Value { get; set; }

        [DataMember]
        public string Message { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadItemID")]
        public virtual LoadItem LoadItem { get; set; }


        [IgnoreDataMember, ForeignKey("LoadTypeRuleID")]
        public virtual LoadTypeRule LoadTypeRule { get; set; }
    }
}
