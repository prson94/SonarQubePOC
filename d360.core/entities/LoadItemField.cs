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
    public class LoadItemField : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public int LoadItemID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int LoadTypeFieldID { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string LookupObjectType { get; set; }

        [DataMember]
        public int? LookupObjectID { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("LoadItemID")]
        public virtual LoadItem LoadItem { get; set; }


        [IgnoreDataMember, ForeignKey("LoadTypeFieldID")]
        public virtual LoadTypeField LoadTypeField { get; set; }
    }
}
