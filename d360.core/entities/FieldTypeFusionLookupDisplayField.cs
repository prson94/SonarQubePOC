using d360.core.entities.Contracts;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeFusionLookupDisplayField : BaseIntObject, IIntObject
    {
        [DataMember]
        public int FieldTypeFusionLookupDefinitionID { get; set; }

        [DataMember]        
        public int FieldTypeID { get; set; }

        [DataMember]
        public string FieldTypeName { get; set; }

        [DataMember]
        public bool Show { get; set; }

        [DataMember]
        public int? SortOrder { get; set; }

        [DataMember]
        public string FilterValue { get; set; }
    }
}
