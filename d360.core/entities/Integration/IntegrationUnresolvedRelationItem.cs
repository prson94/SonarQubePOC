using d360.core.enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("UnresolvedRelationItem", Schema = "integration")]
    public class IntegrationUnresolvedRelationItem : BaseGuidObject
    {
        [DataMember]
        public string SubjectSourceID { get; set; }

        [DataMember]
        public string ObjectSourceID { get; set; }

        [DataMember]
        public int IntersectTypeID { get; set; }

        [DataMember]
        public int AttemptCount { get; set; }

        [DataMember]
        public DateTime MostRecentAttemptOn { get; set; }
    }
}
