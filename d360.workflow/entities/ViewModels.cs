using d360.core;
using d360.core.entities;
using d360.core.entities.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.workflow.entities
{
    [DataContract(Name = "", Namespace = constants.NAMESPACE)]
    public class WorkflowViewModel : BaseGuidObject, IGuidObject
    {
        public WorkflowViewModel()
        {
            Fields = new List<Property>();
        }

        [DataMember]
        public WorkflowType WorkflowType { get; set; }

        [DataMember]
        public string WorkflowTypeName { get; set; }

        [DataMember]
        public string WorkflowTypeDescription { get; set; }

        [DataMember]
        public List<Property> Fields { get; set; }

        [DataMember]
        public DateTime DateStarted { get; set; }

        [DataMember]
        public DateTime? DateCompleted { get; set; }

        [DataMember]
        public List<WorkflowResourceViewModel> Assignments { get; set; }

        [DataMember]
        public List<WorkflowStatusViewModel> Steps { get; set; }
    }

    [DataContract(Name = "Assignment", Namespace = constants.NAMESPACE)]
    public class WorkflowResourceViewModel : BaseObject
    {
        [DataMember]
        public ActivityType ActivityType { get; set; }

        [DataMember]
        public string ActivityTypeName { get; set; }

        [DataMember]
        public string ActivityTypeDescription { get; set; }

        [DataMember]
        public int ResourceID { get; set; }

        [DataMember]
        public string ResourceName { get; set; }

        [DataMember]
        public bool IsComplete { get; set; }
    }

    [DataContract(Name = "Step", Namespace = constants.NAMESPACE)]
    public class WorkflowStatusViewModel : BaseObject
    {
        [DataMember]
        public long ID { get; set; }

        [DataMember]
        public string TraceLevel { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTime Date { get; set; }
    }
}
