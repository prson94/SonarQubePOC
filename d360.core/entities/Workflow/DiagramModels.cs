using d360.core.enums.Workflow;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Runtime.Serialization;
using System.Xml.Linq;

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

        [DataMember, NotMapped]
        public dynamic SettingsObject { get; set; }

        public void ParseSettings()
        {
            if (Settings == null) return;

            XDocument xml = XDocument.Parse(Settings);
            string json = JsonConvert.SerializeXNode(xml);
            SettingsObject = JsonConvert.DeserializeObject<ExpandoObject>(json);
        }
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

        [DataMember, NotMapped]
        public dynamic ConditionObject { get; set; }

        public void ParseCondition()
        {
            if (Condition == null) return;

            XDocument xml = XDocument.Parse(Condition);
            string json = JsonConvert.SerializeXNode(xml);
            ConditionObject = JsonConvert.DeserializeObject<ExpandoObject>(json);
        }
    }
}
