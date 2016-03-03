using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeRelationLookupDefinition : BaseIntObject, IIntObject
    {
        [DataMember]
        public int? IntersectTypeID { get; set; }

        [DataMember]
        public int? ChildIntersectTypeID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public int ReferenceType { get; set; }

        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool HideFooter { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual FieldType FieldType { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeRelationLookupDefinitionID")]
        public virtual ICollection<FieldTypeRelationLookupDisplayField> FieldTypeRelationLookupDisplayFields { get; set; }
    }
}
