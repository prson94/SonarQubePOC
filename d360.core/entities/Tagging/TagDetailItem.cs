#nullable enable
using System;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class TagDetailItem
    {
        [DataMember]
        public Guid uid { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public Guid? CreatedByUid { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public string? CreatedByFirstName { get; set; }

        [DataMember]
        public string? CreatedByLastName { get; set; }
    }
}
