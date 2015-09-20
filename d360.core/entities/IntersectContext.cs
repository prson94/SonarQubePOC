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
    [DataContract(Namespace = NAMESPACE), Table("IntersectContext2")]
    public class IntersectContext : BaseCompanyObject, ICompanyObject, IIntObject
    {
        //[DataMember]
        //[Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        //public string Description { get; set; }

        //[DataMember]
        //public int? IntersectID { get; set; }

        //[DataMember]
        //public int? SourceIntersectContextID { get; set; }

        [DataMember]
        public int? IntersectClassificationID { get; set; }

        //[DataMember]
        //public int? IntersectRoleID { get; set; }

        //[ForeignKey("CompanyID, IntersectID")]
        //public virtual Intersect Intersect { get; set; }

        //[ForeignKey("CompanyID, SourceIntersectContextID")]
        //public virtual IntersectContext SourceContext { get; set; }

        //[ForeignKey("CompanyID, IntersectClassificationID")]
        //public virtual IntersectClassification Classification { get; set; }

        //[ForeignKey("CompanyID, IntersectRoleID")]
        //public virtual IntersectRole Role { get; set; }

        //[ForeignKey("CompanyID, IntersectContextID")]
        //public virtual ICollection<IntersectContextRelation> IntersectContextRelations { get; set; }

        [IgnoreDataMember]
        public virtual ICollection<IntersectContextNodeGroup> IntersectContextNodeIntersectContexts { get; set; }
    }
}
