using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.core.entities.Contracts;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace d360.core.entities
{
    [DataContract(Namespace = NAMESPACE), Table("Execution", Schema = "fusion")]
    public class FusionExecution : BaseIntObject, IIntObject
    {
        [DataMember]
        public Guid QueueID { get; set; }

        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public string RawLogFileName { get; set; }

        [DataMember]
        public DateTime? DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public int? Adds { get; set; }

        [DataMember]
        public int? Updates { get; set; }

        [DataMember]
        public int? Deletes { get; set; }

        [DataMember]
        public bool? LoadIsNew { get; set; }

        [DataMember]
        public DateTime DateToUseForHistory { get; set; }
    }
}
