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
    public class IntersectContextRelation : BaseObject, ICompanyObject
    {
        [Key, Column(Order = 1)]
        public int CompanyID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public int IntersectID { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int IntersectContextID { get; set; }

        [DataMember, Key, Column(Order = 4)]
        public int IntersectRoleID { get; set; }

        [DataMember]
        public int? IntersectClassificationID { get; set; }

        [ForeignKey("CompanyID, IntersectID")]
        public virtual Intersect Intersect { get; set; }

        [ForeignKey("CompanyID, IntersectContextID")]
        public virtual IntersectContext IntersectContext { get; set; }

        [ForeignKey("CompanyID, IntersectRoleID")]
        public virtual IntersectRole IntersectRole { get; set; }

        [ForeignKey("CompanyID, IntersectClassificationID")]
        public virtual IntersectClassification IntersectClassification { get; set; }
    }
}
