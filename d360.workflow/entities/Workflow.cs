using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using d360.core.entities;
using d360.core;

namespace d360.workflow.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class Workflow : BaseGuidObject, IGuidObject
    {
        [DataMember]
        public WorkflowType WorkflowType { get; set; }

        [DataMember]
        public string Data { get; set; }

        [DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public virtual ICollection<WorkflowResource> WorkflowResources { get; set; }

        [DataMember]
        public virtual ICollection<WorkflowStatus> WorkflowStatuses { get; set; }
    }
}
