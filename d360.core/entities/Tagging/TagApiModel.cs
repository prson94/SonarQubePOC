#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    public class TagApiModel
    {

        [DataMember]
        public Guid uid { get; set; }

        [DataMember, StringLength(250)]
        public string Value { get; set; }

        [DataMember]
        public int UseCount { get; set; }

        [DataMember]
        public Guid? CreatedByUid { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }

        [DataMember]
        public Guid? UpdatedByUid { get; set; }

        [DataMember]
        public DateTime UpdatedOn { get; set; }

        [DataMember]
        public string? CreatedByFirstName { get; set; }

        [DataMember]
        public string? CreatedByLastName { get; set; }
    }
}
