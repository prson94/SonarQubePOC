using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities.Contracts;
using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Xml.Serialization;
using d360.core.entities;
using d360.core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace d360.workflow.entities
{
    [DataContract(Namespace = constants.NAMESPACE)]
    public class WorkflowResource : BaseObject
    {
        [DataMember, Key, Column(Order=1)]
        public Guid WorkflowID { get; set; }

        [DataMember, Key, Column(Order = 2)]
        public ActivityType Activity { get; set; }

        [DataMember, Key, Column(Order = 3)]
        public int ResourceID { get; set; }

        [DataMember]
        public bool IsComplete { get; set; }
    }
}
