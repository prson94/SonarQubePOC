using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.entities.Contracts;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("EntityFieldType", Schema = "api")]
    public class ApiEntityFieldType : BaseIntObject, IIntObject
    {
        [DataMember]
        public int EntityID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public string JsonFieldNameOverride { get; set; }

        [DataMember]
        public string XmlFieldNameOverride { get; set; }

        [DataMember]
        public bool AllowSelect { get; set; }

        [DataMember]
        public bool AllowSort { get; set; }

        [DataMember]
        public bool AllowFilter { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string ItemNameOverride { get; set; }

        [NotMapped]
        [DataMember]
        public List<ApiEntityFieldTypeMultiSelectField> MultiSelectFields { get; set; }

        [ForeignKey("EntityID"), IgnoreDataMember]
        public virtual ApiEntity Entity { get; set; }

        [ForeignKey("FieldTypeID"), IgnoreDataMember]
        public virtual FieldType FieldType { get; set; }
    }
}
