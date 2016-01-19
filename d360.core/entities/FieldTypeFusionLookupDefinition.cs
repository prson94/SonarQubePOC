using System.Collections.Generic;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class FieldTypeFusionLookupDefinition : BaseIntObject, IIntObject
    {
        [DataMember]
        public string Display { get; set; }

        [DataMember]
        public int TargetFusionAttributeTypeID { get; set; }

        [DataMember]
        public int SourceFusionAttributeTypeID { get; set; }

        [DataMember]
        public int FieldTypeID { get; set; }

        [DataMember]
        public bool IsParentChild { get; set; }
    }
}
