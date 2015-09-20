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
    [DataContract(Namespace = NAMESPACE)]
    public class FusionStatusLog : BaseGuidObject, IGuidObject
    {
        [DataMember]
        public int FusionID { get; set; }

        [DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public string MachineQueuedOn { get; set; }

        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }

        [IgnoreDataMember]
        public virtual Fusion Fusion { get; set; }
    }
}
