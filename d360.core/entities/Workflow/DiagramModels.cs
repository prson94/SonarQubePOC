using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using d360.core.enums.Workflow;

namespace d360.core.entities.Workflow
{

    public class WorkflowDiagramModel
    {
        [DataMember]
        public Type Type { get; set; }
        
        [DataMember]
        public WorkflowEventRegistration Event { get; set; }
        
        [DataMember]
        public List<WorkflowDiagramNode> Nodes { get; set; } = new List<WorkflowDiagramNode>();
        
        [DataMember]
        public List<WorkflowDiagramLink> Links { get; set; } = new List<WorkflowDiagramLink>();
        
        [DataMember]
        public WorkflowVersion CurrentVersion { get; set; }
        
        [DataMember]
        public WorkflowVersion PublishedVersion { get; set; }
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
        public WorkflowActivityType ActivityType { get; set; }

        [DataMember]
        public string Settings { get; set; }

        [DataMember]
        public string Fields { get; set; }

        [DataMember, NotMapped]
        public dynamic FieldsObject { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember, NotMapped]
        public dynamic SettingsObject { get; set; }

        [DataMember, NotMapped]
        public int RunCount { get; set; }
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
        public string FromPortID { get; set; }
        
        [DataMember]
        public string ToPortID { get; set; }
        
        [DataMember]
        public TransitionType TransitionType { get; set; }
        
        [DataMember]
        public string Condition { get; set; }
        
        [DataMember]
        public string Settings { get; set; }
        
        [DataMember]
        public string Name { get; set; }

        [DataMember, NotMapped]
        public dynamic ConditionObject { get; set; }

        [DataMember, NotMapped]
        public dynamic SettingsObject { get; set; } 
    }
}
