using d360.core.enums;
using d360.core.enums.Workflow;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.entities.Workflow
{
    public class FieldModel
    {
        [JsonProperty(PropertyName = "label", Order = 3)]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type", Order = 2)]
        public string Type { get; set; }
    }

    public class FieldValueModel
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value", Order = 2)]
        public string Value { get; set; }
    }

    public class SettingModel
    {
        [JsonProperty(PropertyName = "name", Order = 1)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value", Order = 2)]
        public string Value { get; set; }
    }

    public class Conditions
    {
        [JsonProperty(PropertyName = "conjunction", Order = 1)]
        public string Conjunction { get; set; }

        [JsonProperty(PropertyName = "field", Order = 2)]
        public List<FieldValueModel> Fields { get; set; }

        [JsonProperty(PropertyName = "setting", Order = 3)]
        public List<SettingModel> Settings { get; set; }
    }

    public class WorkflowTypeModel
    {
        public Type Type { get; set; } = new Type();
        public WorkflowEventRegistration Event { get; set; } = new WorkflowEventRegistration();
    }

    public class WorkflowStepDetail
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public int TypeID { get; set; }

        [DataMember]
        public int ItemID { get; set; }

        [DataMember]
        public int StepID { get; set; }

        [DataMember]
        public StepType StepType { get; set; }

        [DataMember]
        public WorkflowActivityType ActivityType { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string SettingsXml { get; set; }

        [DataMember]
        public string FieldsXml { get; set; }

        [DataMember]
        public string ItemSettingsXml { get; set; }

        [DataMember]
        public string ItemFieldsXml { get; set; }

        [DataMember]
        public string Object { get; set; }

        [DataMember]
        public int ObjectID { get; set; }

        [DataMember]
        public string ObjectType { get; set; }

        [DataMember]
        public int ObjectTypeID { get; set; }
        [DataMember]
        public string ConditionXml { get; set; }

        [DataMember]
        public string EventSettingsXml { get; set; }

        [DataMember]
        public dynamic Condition { get; set; }

        [DataMember]
        public dynamic EventSettings { get; set; }

        [DataMember]
        public ChangeType ChangeType { get; set; }

        [DataMember]
        public DateTime? StartedOn { get; set; }

        [DataMember]
        public DateTime? CompletedOn { get; set; }

        [DataMember]
        public dynamic Settings { get; set; }

        [DataMember]
        public dynamic Fields { get; set; }

        [DataMember]
        public dynamic ItemSettings { get; set; }

        [DataMember]
        public dynamic ItemFields { get; set; }

        [DataMember]
        public List<GlobalReportingResource> ResponsibleUsers { get; set; }

        [DataMember]
        public bool IsIssueType { get; set; }
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public bool IsPublishedVersion { get; set; }

        [DataMember]
        public WorkflowStepIssueDetail IssueDetails { get; set; }
    }

    public class WorkflowStepIssueDetail
    {
        public int ID { get; set; }
        public int IssueID { get; set; }
        public int IssueTypeID { get; set; }
        public IssueCriticality Criticality { get; set; }
        public string IssueName { get; set; }
        public string ObjectName { get; set; }
        public string ObjectTypeName { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public string ObjectType { get; set; }
        public int ObjectTypeID { get; set; }
    }

}
