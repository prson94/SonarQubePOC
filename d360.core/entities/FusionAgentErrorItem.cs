using System;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("AgentErrorItem", Schema = "fusion")]
    public class FusionAgentErrorItem : BaseIntObject
    {
        [DataMember]
        public int AgentErrorID { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public DateTime Date { get; set; }
    }
}