using d360.core;
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

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
        relationshipType
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
    }

    public class BulkWorkflowFormModel
    {
        public List<long> ItemStepIDs { get; set; } = new List<long>();
        public List<WorkflowFormModelField> Fields { get; set; } = new List<WorkflowFormModelField>();
}
    
}