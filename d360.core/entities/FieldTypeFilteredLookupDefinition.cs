using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeFilteredLookupDefinition : BaseIntObject, IIntObject
    {
        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public bool HideHeader { get; set; }

        [DataMember]
        public bool HideFooter { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeID")]
        public virtual FieldType FieldType { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeFilteredLookupDisplayFieldID")]
        public virtual ICollection<FieldTypeFilteredLookupDisplayField> FieldTypeFilteredLookupDisplayFields { get; set; }
    }
}
