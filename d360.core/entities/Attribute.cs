using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using d360.core.entities.Contracts;
using System.ComponentModel;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), ObjectType(d360.core.ObjectTypeInfo.Attribute, "Attribute")]
    public partial class Attribute : BaseIntObject, IIntObject, IFieldsObject, ISearchable, IUpdatedMetadata
    {
        [DataMember]
        public int AttributeTypeID { get; set; }

        public int ObjectID { get; set; }

        public string ObjectType { get; set; }
        
        public int? InheritanceObjectID { get; set; }

        public string InheritanceObjectType { get; set; }

        [DataMember]
        public int? ParentID { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        #region Properties

        [XmlIgnore()]
        public virtual AttributeType AttributeType { get; set; }

        [XmlIgnore()]
        public virtual Attribute Parent { get; set; }

        [DataMember]
        [ForeignKey("ParentID")]
        public virtual List<Attribute> Children { get; set; }

        #endregion
    }
}
