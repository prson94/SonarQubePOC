using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class MapRule : BaseIntObject, IIntObject, ICreatedObject, ICreatedMetadata, IUpdatedMetadata
    {
        [DataMember]
        public string Transformation { get; set; }

        [DataMember]
        public DateTime? CreatedOn { get; set; }

        [DataMember]
        public int? CreatedBy { get; set; }

        [DataMember]
        public DateTime? UpdatedOn { get; set; }

        [DataMember]
        public int? UpdatedBy { get; set; }
        
        [DataMember]
        public virtual ICollection<MapRuleItem> MapRuleItems { get; set; }
    }
}
