using d360.core.enums.Workflow;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace d360.core.entities.Workflow
{

    public class WorkflowDiagramModel
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<WorkflowDiagramNode> Nodes { get; set; } = new List<WorkflowDiagramNode>();
        [DataMember]
        public List<WorkflowDiagramLink> Links { get; set; } = new List<WorkflowDiagramLink>();
    }


    public class WorkflowDiagramNode
    {
        [DataMember]
        public string Key { get; set; }
        [DataMember]
        public int XPosition { get; set; }
        [DataMember]
        public int YPosition { get; set; }
        [DataMember]
        public StepType StepType { get; set; }
        [DataMember]
        public int ActivityType { get; set; }
        [DataMember]
        public string Settings { get; set; }
        [DataMember]
        public string Name { get; set; }
    }

    public class WorkflowDiagramLink
    {
        [DataMember]
        public string Key { get; set; }
        [DataMember]
        public string FromKey { get; set; }
        [DataMember]
        public string ToKey { get; set; }
        [DataMember]
        public TransitionType TransitionType { get; set; }
        [DataMember]
        public LinkType LinkType { get; set; }
        [DataMember]
        public string Condition { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
}
