using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeRelationLookupDisplayField : BaseIntObject, IIntObject
    {
        [DataMember]
        public int FieldTypeRelationLookupDefinitionID { get; set; }

        [DataMember]        
        public int FieldTypeID { get; set; }

        [DataMember]
        public string FieldTypeName { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeRelationLookupDefinitionID")]
        public virtual FieldTypeRelationLookupDefinition FieldTypeRelationLookupDefinition { get; set; }
    }
}
