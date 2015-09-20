using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IntersectContextDomainListItem : BaseObject, ICompanyObject
    {
        [DataMember, Key, Column(Order=1)]
        public int CompanyID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int DomainListItemID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int IntersectContextID { get; set; }

        [ForeignKey("CompanyID, DomainListItemID")]
        public virtual DomainListItem DomainListItem { get; set; }

        [ForeignKey("CompanyID, IntersectContextID")]
        public virtual IntersectContext IntersectContext { get; set; }
    }
}
