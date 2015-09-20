using System.Collections.Generic;
using d360.core.entities.Contracts;
using System;
using System.Xml.Linq;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(ObjectTypeInfo.AttributeType, "AttributeType")]
    public class AttributeType : BaseIntObject, IIntObject, IUpdatedMetadata
    {
        #region Properties

        [DataMember]
        public int? ParentID { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Name_Name", Description = "Name_Description")]
        public string Name { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "Description_Name", Description = "Description_Description")]
        public string Description { get; set; }

        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "TextFormatString_Name", Description = "TextFormatString_Description")]
        public string TextFormatString { get; set; }

        [DataMember]
        [Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AttributeTypeCategory_Name", Description = "AttributeTypeCategory_Description")]
        public int? AttributeTypeCategoryID { get; set; }

        [IgnoreDataMember]
        public virtual AttributeType Parent { get; set; }

        [
        ForeignKey("AttributeTypeCategoryID"), IgnoreDataMember,
        Display(ResourceType = typeof(d360.core.resources.Fields), Name = "AttributeTypeCategory_Name", Description = "AttributeTypeCategory_Description")
        ]
        public virtual AttributeTypeCategory AttributeTypeCategory { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #endregion

        #region Collection Properties

        [ForeignKey("ParentID"), IgnoreDataMember]
        public virtual ICollection<AttributeType> Children { get; set; }

        [ForeignKey("AttributeTypeID"), IgnoreDataMember]
        public virtual ICollection<Attribute> Attributes { get; set; }

        [ForeignKey("AttributeTypeID"), IgnoreDataMember]
        public virtual ICollection<AttributeTypeRelation> Relations { get; set; }

        #endregion
    }
}
