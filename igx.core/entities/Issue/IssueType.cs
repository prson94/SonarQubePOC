using d360.core.entities.Contracts;
using System;
using System.Data;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE)]
    public class IssueType : BaseIntObject, IIntObject
    {
        [DataMember, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid uid { get; set; }

        [DataMember, StringLength(250)]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }

        [DataMember]
        public bool IsSystem { get; set; }

    }
}
