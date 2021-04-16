using System;
using d360.core.entities.Contracts;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("ExecutionsExternal", Schema = "api")]
    public class ApiExecutionsExternal : BaseIntObject, IIntObject
    {
        [DataMember]
        public Guid ExternalID { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(50)]
        public string Status { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Detail { get; set; }

        [DataMember]
        [Column(TypeName = "varchar"), StringLength(250)]
        public string Component { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }
    }


    public class ApiExecutionExternalRequestModel
    {
        public string Status { get; set; }
        public Guid? Uid { get; set; }
        public string Detail { get; set; }
        public string Component { get; set; }
    }

    public class ApiExecutionExternalViewModel
    {
        public string Status { get; set; }
        public Guid Uid { get; set; }
        public string Detail { get; set; }
        public string Component { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
