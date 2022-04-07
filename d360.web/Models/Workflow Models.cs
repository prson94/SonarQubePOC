using System.Collections.Generic;
using System.Web.Mvc;

namespace d360.web.Models
{
    public enum WorkflowFormModelFieldType
    {
        text = 0,
        boolean,
        integer,
        date,
        textarea,
        list,
        relationshipType,
        html,
        link,
    }

    public class WorkflowFormModelField
    {
        public string Label { get; set; }

        public WorkflowFormModelFieldType FieldType { get; set; }

        public object Value { get; set; }

        public string ID { get; set; }

        public string ReferenceFieldID { get; set; }

        public List<SelectListItem> Values { get; set; }

        public bool AllowMultipleValues { get; set; }

        public int IntersectTypeID { get; set; }

        public bool Required { get; set; }
    }

    public class BulkWorkflowFormModel
    {
        public List<long> ItemStepIDs { get; set; } = new List<long>();

        public List<WorkflowFormModelField> Fields { get; set; } = new List<WorkflowFormModelField>();
    }

    public class BulkWorkflowReassignModel
    {
        public List<long> ItemStepIDs { get; set; } = new List<long>();

        public bool SendFormEmails { get; set; } = true;

        public int NewAssigneeResourceID { get; set; }

        public int OriginalAssigneeResourceID { get; set; }

        public bool ClearOtherAssignments { get; set; }
    }
}
