using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using d360.core.entities.Contracts;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public partial class Attribute : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata
    {
        [DataMember]
        public int AttributeTypeID { get; set; }

        public int ObjectID { get; set; }

        [Column(TypeName = "varchar"), StringLength(50)]
        public string ObjectType { get; set; }
        
        public int? InheritanceObjectID { get; set; }

        [Column(TypeName = "varchar"), StringLength(50)]
        public string InheritanceObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        
        [XmlIgnore()]
        public virtual AttributeType AttributeType { get; set; }

        [XmlIgnore()]
        public virtual Attribute Parent { get; set; }

        [DataMember]
        [ForeignKey("ParentID")]
        public virtual List<Attribute> Children { get; set; }

        public FieldsObjectModel GetFieldsObjectInfo()
        {
            return new FieldsObjectModel { Type = SystemObjects.AttributeType, Object = SystemObjects.Attribute, TypeID = AttributeTypeID };
        }
    }
}
