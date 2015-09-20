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
using d360.core.enums;

namespace d360.core.entities.Views
{
    [DataContract(Namespace = NAMESPACE)]
    public class SecurityDetail : BaseObject
    {
        #region Properties

        [DataMember, Key, Column(Order = 1)]
        public string ResponsibleObjectType { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int ResponsibleObjectID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public string ObjectType { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public int ObjectID { get; set; }

        [DataMember, Key, Column(Order = 5)]
        public Claim Claim { get; set; }

        [DataMember, Key, Column(Order = 6)]
        public ClaimObject ClaimObject { get; set; }

        #endregion
    }
}
