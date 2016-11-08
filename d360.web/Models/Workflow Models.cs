using d360.core;
using d360.core.entities;
using d360.workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Linq;

namespace d360.web.Models
{
    /// <summary>
    /// A workflow breakdown.
    /// </summary>
    [DataContract(Name = "WorkflowBreakdown", Namespace = constants.NAMESPACE)]
    public class WorkflowBreakdown
    {
        [DataMember]
        public int WorkflowTypeID { get; set; }
        [DataMember]
        public string WorkflowTypeName { get; set; }
        [DataMember]
        public WorkflowType Workflow { get; set; }
        [DataMember]
        public int Count { get; set; }
    }

    /// <summary>
    /// An open workflow task.
    /// </summary>
    [DataContract(Name = "WorkflowTask", Namespace = constants.NAMESPACE)]
    public class WorkflowTask
    {
        public WorkflowTask()
        {
            Properties = new Dictionary<string, string>();
        }

        /// <summary>
        /// The instance ID of the workflow that this task is related to.
        /// </summary>
        [DataMember]
        public Guid WorkflowID { get; set; }

        /// <summary>
        /// The workflow type for this instance.
        /// </summary>
        [DataMember]
        public WorkflowType Workflow { get; set; }

        /// <summary>
        /// The name of the workflow type for this instance.
        /// </summary>
        [DataMember, NotMapped]
        public string WorkflowName { get; set; }

        /// <summary>
        /// The description of the workflow type for this instance.
        /// </summary>
        [DataMember, NotMapped]
        public string WorkflowDescription { get; set; }

        /// <summary>
        /// The type of task.
        /// </summary>
        [DataMember]
        public ActivityType Activity { get; set; }

        /// <summary>
        /// The name for this type of task.
        /// </summary>
        [DataMember, NotMapped]
        public string ActivityName { get; set; }

        /// <summary>
        /// The description for this type of task.
        /// </summary>
        [DataMember, NotMapped]
        public string ActivityDescription { get; set; }

        /// <summary>
        /// Contains the raw XML data settings for this workflow.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// The date this task was created.
        /// </summary>
        [DataMember]
        public DateTime DateStarted { get; set; }

        /// <summary>
        /// Contains the hydrated list of key properties for this workflow.
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Properties { get; set; }
    }

    /// <summary>
    /// An open workflow assignment.
    /// </summary>
    [DataContract(Name = "WorkflowAssignment", Namespace = constants.NAMESPACE)]
    public class WorkflowAssignment
    {
        public void Hydrate()
        {
            WorkflowName = Workflow.GetWorkflowTypeDisplayName();
            WorkflowDescription = Workflow.GetWorkflowTypeDescription();
            ActivityName = Activity.GetActivityTypeDisplayName();
            ActivityDescription = Activity.GetReportTileTypeDescription();
            Settings = (
                        from e in XElement.Parse(Data).Elements()
                        where e.Name.LocalName != "RequestingResourceID"
                        select new Property { Name = e.Name.LocalName, Value = e.Value }
                       ).ToList();

            if (!string.IsNullOrEmpty(ArtifactTypeName))
                Settings.Add(new Property { Name = "ArtifactTypeName", Value = ArtifactTypeName });
            if (!string.IsNullOrEmpty(TaxonomyTypeName))
                Settings.Add(new Property { Name = "TaxonomyTypeName", Value = TaxonomyTypeName });
        }

        /// <summary>
        /// The instance ID of the workflow that this task is related to.
        /// </summary>
        [DataMember]
        public Guid WorkflowID { get; set; }

        /// <summary>
        /// The workflow type for this instance.
        /// </summary>
        [DataMember]
        public WorkflowType Workflow { get; set; }

        /// <summary>
        /// The name of the workflow type for this instance.
        /// </summary>
        [DataMember, NotMapped]
        public string WorkflowName { get; set; }

        /// <summary>
        /// The description of the workflow type for this instance.
        /// </summary>
        [DataMember, NotMapped]
        public string WorkflowDescription { get; set; }

        /// <summary>
        /// Contains the raw XML data settings for this workflow.
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// Contains the data settings for this workflow.
        /// </summary>
        [DataMember, NotMapped]
        public List<Property> Settings { get; set; }

        /// <summary>
        /// The date this task was created.
        /// </summary>
        [DataMember]
        public DateTime DateStarted { get; set; }

        /// <summary>
        /// The type of task.
        /// </summary>
        [DataMember]
        public ActivityType Activity { get; set; }

        /// <summary>
        /// The name for this type of task.
        /// </summary>
        [DataMember, NotMapped]
        public string ActivityName { get; set; }

        /// <summary>
        /// The description for this type of task.
        /// </summary>
        [DataMember, NotMapped]
        public string ActivityDescription { get; set; }

        /// <summary>
        /// The ID of the user that made the initial request.
        /// </summary>
        [DataMember]
        public int? RequestingResourceID { get; set; }

        /// <summary>
        /// The full name of the user that made the initial request.
        /// </summary>
        [DataMember]
        public string RequestingResourceName { get; set; }

        /// <summary>
        /// The relative url of the user that made the initial request.
        /// </summary>
        [DataMember]
        public string RequestingResourceUrl { get; set; }

        public string TaxonomyTypeName { get; set; }

        public string ArtifactTypeName { get; set; }
    }

    public class WorkflowRequestModel : Dictionary<string, string>
    {
    }

    public class WorkflowTaskBaseModel
    {
        public string WorkflowName { get; set; }
        public string WorkflowDescription { get; set; }
        public string ActivityName { get; set; }
        public string ActivityDescription { get; set; }
        public Guid WorkflowID { get; set; }
        public ActivityType Activity { get; set; }
    }

    public class WorkflowTask1Model : WorkflowTaskBaseModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public DateTime StartDate { get; set; }
        public string ProposedName { get; set; }
        public string ProposedDescription { get; set; }
        public int RequestingResourceID { get; set; }
        public string RequestingResourceName { get; set; }
        public int TaxonomyTypeID { get; set; }
        public string TaxonomyTypeName { get; set; }
    }

    public class WorkflowTask2Model : WorkflowTaskBaseModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string TypeName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class WorkflowTask3Model : WorkflowTaskBaseModel
    {
        public string Issue { get; set; }
        public int ResourceID { get; set; }
        public string ResourceName { get; set; }
        public string ResourceUrl { get; set; }
        public DateTime DateStarted { get; set; }
        public core.enums.IssueType IssueType { get; set; }
        public string IssueTypeName { get; set; }
    }
    public class WorkflowTask4Model : WorkflowTaskBaseModel
    {
        public string Issue { get; set; }
        public int ResourceID { get; set; }
        public string ResourceName { get; set; }
        public string ResourceUrl { get; set; }
        public DateTime DateStarted { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public string Url { get; set; }
        public int ArtifactID { get; set; }
    }

    public class WorkflowTask5Model : WorkflowTask1Model
    {        
    }

}