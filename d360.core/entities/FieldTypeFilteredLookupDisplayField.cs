using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeFilteredLookupDisplayField : BaseIntObject, IIntObject
    {
        [DataMember]
        public int FieldTypeFilteredLookupDefinitionID { get; set; }

        [DataMember]        
        public int FieldTypeID { get; set; }

        [DataMember]
        public string FieldTypeName { get; set; }

        [DataMember]
        public bool Show { get; set; }

        [DataMember]
        public int? SortOrder { get; set; }

        [DataMember]
        public bool Filter { get; set; }

        [IgnoreDataMember, ForeignKey("FieldTypeFilteredLookupDefinitionID")]
        public virtual FieldTypeFilteredLookupDefinition FieldTypeFilteredLookupDefinition { get; set; }
    }
}
