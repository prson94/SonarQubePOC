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

    
    
    public class WorkflowTaskBaseModel
    {
        public string WorkflowName { get; set; }
        public string WorkflowDescription { get; set; }
        public string ActivityName { get; set; }
        public string ActivityDescription { get; set; }
        public Guid WorkflowID { get; set; }
        public ActivityType Activity { get; set; }
    }

        
    public class WorkflowTask3Model : WorkflowTaskBaseModel
    {
        public string Issue { get; set; }
        public int ResourceID { get; set; }
        public string ResourceName { get; set; }
        public string ResourceUrl { get; set; }
        public DateTime DateStarted { get; set; }
        public int IssueType { get; set; }
        public string IssueTypeName { get; set; }
        public int IssueID { get; set; }
        public core.enums.IssueCriticality Criticality { get; set; }
        public string CriticalityName { get; set; }
        public int EllapsedDays { get; set; }
        public int ObjectID { get; set; }
        public string Object { get; set; }
        public string ObjectName { get; set; }

    }
    
    
    public enum WorkflowFormModelFieldType
    {
        text = 0,
        boolean,
        integer,
        date,
        textarea
    }

    public class WorkflowFormModelField
    {
        public string Label { get; set; }
        public WorkflowFormModelFieldType FieldType { get; set; }

        public object Value { get; set; }

        public string ID { get; set; }
    }
    
}