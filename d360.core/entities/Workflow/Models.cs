using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using d360.core.enums;
using d360.core.enums.Workflow;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace d360.core.entities.Workflow
{
    public class EmailedResourceResponsibility
    {
        public string FullName { get; set; }
        
        public int ResourceID { get; set; }
        
        public string Email { get; set; }
        
        public string Responsibility { get; set; }
    }

    public class WorkflowItemStepDetail
    {
        public int ID { get; set; }
        
        public int ItemID { get; set; }
        
        public int StepID { get; set; }
        
        public string Name { get; set; }
        
        public StepType StepType { get; set; }
        
        public WorkflowActivityType ActivityType { get; set; }
        
        public bool Complete { get; set; }
        
        public DateTime? StartedOn { get; set; }
        
        public DateTime? CompletedOn { get; set; }
        
        public string StartedBy { get; set; }
        
        public string CompletedBy { get; set; }
        
        public string MessageRecipientType { get; set; }
        
        public string Assignee { get; set; }
        
        public string Fields { get; set; }
        
        public bool IsIssueType { get; set; }
        
        public string Object { get; set; }
        
        public int ObjectID { get; set; }
        
        public int TypeID { get; set; }
        
        public string IsAssignedLoginUser { get; set; } = bool.FalseString;
        
        public FieldsModel FieldsObject { get; set; }

        [Serializable, XmlRoot("fields")]
        public class FieldsModel
        {
            [XmlAttribute("NumberOfResponses")]
            public int NumberOfResponses { get; set; }
            
            [XmlAttribute("TotalResources")]
            public int TotalResources { get; set; }

            [XmlElement("Reassigned")]
            public List<ReassignmentDetail> Reassignments { get; set; }

            [XmlElement("form")]
            public List<FormDetail> Forms { get; set; }

            public class FormDetail
            {
                [JsonProperty(PropertyName = "@ResourceID")]
                public int ResourceID { get; set; }
            }

            public class ReassignmentDetail
            {
                [XmlAttribute("reassignType")]
                public string ReassignType { get; set; }
                
                [XmlAttribute("objectId")]
                public int ObjectID { get; set; }
                
                [XmlAttribute("objectType")]
                public string ObjectType { get; set; }
                
                [XmlAttribute("objectName")]
                public string ObjectName { get; set; }
                
                [XmlAttribute("byResourceId")]
                public int ByResourceID { get; set; }
                
                [XmlAttribute("toResourceId")]
                public int ToResourceID { get; set; }
                
                [XmlAttribute("fromResourceId")]
                public int FromResourceID { get; set; }
                
                [XmlAttribute("byResourceName")]
                public string ByResourceName { get; set; }
                
                [XmlAttribute("fromResourceName")]
                public string FromResourceName { get; set; }
                
                [XmlAttribute("toResourceName")]
                public string ToResourceName { get; set; }
                
                [XmlAttribute("reassignOn")]
                public string ReassignOn { get; set; }
            }
        }
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
        public int ItemStepID { get; set; }

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
        public string ObjectName { get; set; }

        [DataMember]
        public string ObjectTypeName { get; set; }

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
        public int StartedBy { get; set; }

        [DataMember]
        public int CompletedBy { get; set; }

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
        public List<GlobalReportingResource> AssignedUsers { get; set; }

        [DataMember]
        public bool IsAssignedLoginUser { get; set; }
        [DataMember]
        public bool IsIssueType { get; set; }
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public bool IsPublishedVersion { get; set; }

        [DataMember]
        public WorkflowStepIssueDetail IssueDetails { get; set; }
        
        [DataMember]
        public List<WorkflowStepFieldChange> FieldChanges { get; set; }

        public WorkflowStepRelationshipChange RelationshipChange { get; set; }

        public State StateChange { get; set; }

        public int AssetId { get; set; }
    }

    public class WorkflowStepFieldChange
    {
        [DataMember]
        public string FieldName { get; set; }
        
        [DataMember]
        public string Asset { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Value { get; set; }
        
        [DataMember]
        public bool UseCurrentDate { get; set; }
        
        [DataMember]
        public bool UseOutputValue { get; set; }
        
        [DataMember]
        public bool FormValue { get; set; }

        [DataMember]
        public string AppendValue { get; set; }

        [DataMember]
        public string ClearValue { get; set; }

        [DataMember]
        public string ObjectType { get; set; }
    }

    public class WorkflowStepRelationshipChange
    {
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string Relationship { get; set; }

        [DataMember]
        public bool AppendValue { get; set; }

        [DataMember]
        public bool ClearValue { get; set; }
    }

    public class WorkflowStepIssueDetail
    {
        public int ID { get; set; }
        
        public int IssueID { get; set; }
        
        public int IssueTypeID { get; set; }
        
        public string IssueName { get; set; }
        
        public string ObjectName { get; set; }
        
        public string ObjectTypeName { get; set; }
        
        public string Object { get; set; }
        
        public int ObjectID { get; set; }
        
        public string ObjectType { get; set; }
        
        public int ObjectTypeID { get; set; }
        
        public int AssetId { get; set; }
    }

    #region API View Model

    public class WorkflowTypeApiViewModel
    {
        public Guid? WorkflowTypeUid { get; set; }
        
        public Guid? ActionTypeUid { get; set; }
        
        public string ActionType { get; set; }
        
        public Guid? AssetTypeUid { get; set; }
        
        public string AssetType { get; set; }
        
        public Guid? RelationshipTypeUid { get; set; }
        
        public string RelationshipType { get; set; }
        
        public string Name { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public State State { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public ChangeType ChangeType { get; set; }
        
        public string Description { get; set; }
        
        public string Type { get; set; }
        
        public Guid? PublishedVersionUid { get; set; }
        
        public int? PublishedVersion { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public DateTime UpdatedOn { get; set; }
        
        public string CreatedBy { get; set; }
        
        public string UpdatedBy { get; set; }
    }

    public class WorkflowVersionApiViewModel
    {
        public Guid? Uid { get; set; }
        
        public Guid? ActionTypeUid { get; set; }
        
        public Guid? AssetTypeUid { get; set; }
        
        public Guid? RelationshipTypeUid { get; set; }
        
        public Guid WorkflowTypeUid { get; set; }
        
        public int VersionNumber { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public State State { get; set; }
        
        public bool IsPublished { get; set; }
        
        public Guid CreatedByUid { get; set; }
        
        public DateTime CreatedOn { get; set; }
        
        public Guid UpdatedByUid { get; set; }
        
        public DateTime UpdatedOn { get; set; }
        
        public int TotalWorkflowItems { get; set; }
        
        public int TotalPendingWorkflowItems { get; set; }
    }

    public class WorkflowsApiViewModel
    {
        [DataMember]
        public int pageSize { get; set; } = 250;
        
        [DataMember]
        public int pageNum { get; set; } = 1;
        
        [DataMember]
        public int total { get; set; } = 0;
        
        [DataMember]
        public IEnumerable<WorkflowApiViewModel> items { get; set; }
    }

    public enum WorkflowApiState
    {

        [Name("Active")]
        Active = 1,
        
        [Name("InActive")]
        InActive = 0

    }

    public class WorkflowApiViewModel
    {
        public Guid? Uid { get; set; }
        
        public Guid? ActionUid { get; set; }
        
        public Guid? AssetUid { get; set; }
        
        public Guid? RelationshipUid { get; set; }
        
        public Guid WorkflowTypeUid { get; set; }
        
        public Guid WorkflowVersionUid { get; set; }
        
        public DateTime StartedOn { get; set; }
        
        public Guid StartedByUid { get; set; }
        
        public DateTime? CompletedOn { get; set; }
        
        public Guid? CompletedByUid { get; set; }
    }

    public class WorkflowVersionsApiViewModel
    {
        [DataMember]
        public int pageSize { get; set; } = 250;
        
        [DataMember]
        public int pageNum { get; set; } = 1;
        
        [DataMember]
        public int total { get; set; } = 0;
        
        [DataMember]
        public IEnumerable<WorkflowVersionApiViewModel> items { get; set; }
    }

    public class WorkflowVersionStepsApiViewModel
    {
        public Guid Uid { get; set; }
        
        public string Name { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public State State { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public StepType StepType { get; set; }

        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public WorkflowActivityType ActivityType { get; set; }
        
        public dynamic Settings { get; set; }
        
        [JsonIgnore]
        public string SettingsXml { get; set; }
        
        [JsonIgnore]
        public string FieldsXml { get; set; }
        
        public Guid? StartedByUid { get; set; }
        
        public DateTime? StartedOn { get; set; }
        
        public Guid? CompletedByUid { get; set; }
        
        public DateTime? CompletedOn { get; set; }
    }

    public class WorkflowInstanceApiViewModel
    {
        [JsonIgnore]
        public int ID { get; set; }
        
        public Guid Uid { get; set; }
        
        public string Name { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public State State { get; set; }
        
        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public StepType StepType { get; set; }

        [DataMember, JsonConverter(typeof(StringEnumConverter))]
        public WorkflowActivityType ActivityType { get; set; }
        
        public dynamic Settings { get; set; }

        public dynamic Responses { get; set; }
        
        [JsonIgnore]
        public string SettingsXml { get; set; }
        
        [JsonIgnore]
        public string FieldsXml { get; set; }

        [JsonIgnore]
        public string ItemSettings { get; set; }
        
        [JsonIgnore]
        public string ItemFields { get; set; }
        
        public IList<WorkflowAssignmentApiViewModel> Assignments { get; set; }

        public Guid? StartedByUid { get; set; }
        
        public DateTime? StartedOn { get; set; }
        
        public Guid? CompletedByUid { get; set; }
        
        public DateTime? CompletedOn { get; set; }
    }

    public class WorkflowAssignmentApiViewModel
    {
        public Guid AssigneeUid { get; set; }
    }

    public class WorkflowReassignmentAssetApiModel
    {
        public int ID { get; set; }
        
        public string Name { get; set; }
        
        public string Object { get; set; }
        
        public string ObjectID { get; set; }
    }

    #endregion
}
