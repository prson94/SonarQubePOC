using d360.core;
using d360.core.entities;
using d360.core.enums;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.web.Models
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class AddRelationshipsModel
    {
        [DataMember]
        public IntersectClassification Classification { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public List<ObjectModel> Targets { get; set; }
    }
}