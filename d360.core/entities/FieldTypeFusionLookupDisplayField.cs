using d360.core.entities.Contracts;
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
    }
}
