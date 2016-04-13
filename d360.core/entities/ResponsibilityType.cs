using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using d360.core.enums;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.ResponsibilityType, "ResponsibilityType")]
    public class ResponsibilityType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember, Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description"), StringLength(250)]
        public string Name { get; set; }

        [
        DataMember, 
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "ResponsibilityTypeGroup_Name", Description = "ResponsibilityTypeGroup_Description")
        ]
        public ResponsibilityTypeGroup ResponsibilityTypeGroup { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        [IgnoreDataMember, ForeignKey("ResponsibilityTypeID")]
        public virtual ICollection<ResponsibilityTypeClaim> ResponsibilityTypeClaims { get; set; }

        [IgnoreDataMember, ForeignKey("ResponsibilityTypeID")]
        public virtual ICollection<ResponsibilityTypeObjectClaim> ResponsibilityTypeObjectClaims { get; set; }

        [IgnoreDataMember, ForeignKey("ResponsibilityTypeID")]
        public virtual ICollection<ResponsibilityTypeRelation> ResponsibilityTypeRelations { get; set; }

        [IgnoreDataMember, ForeignKey("ResponsibilityTypeID")]
        public virtual ICollection<Responsibility> Responsibilities { get; set; }
    }
}
