using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AgentError", Schema = "fusion")]
    public class FusionAgentError : BaseIntObject, IIntObject
    {        
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public string MachineName { get; set; }

        [DataMember]
        public DateTime Date { get; set; }

        [IgnoreDataMember, ForeignKey("AgentErrorID")]
        public virtual ICollection<FusionAgentErrorItem> FusionAgentErrorItems { get; set; }
    }
}
