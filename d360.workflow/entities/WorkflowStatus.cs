using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using d360.core.entities;
using System.Diagnostics;
using d360.core;

namespace d360.workflow.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class WorkflowStatus : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public Guid WorkflowID { get; set; }

        [DataMember]
        public TraceLevel TraceLevel { get; set; }

        [DataMember]
        public int RecordNumber { get; set; }

        [DataMember]
        public string ActivityName { get; set; }

        [DataMember]
        public string ActivityState { get; set; }

        [DataMember]
        public string Data { get; set; }

        [DataMember]
        public DateTime Date { get; set; }
    }
}
