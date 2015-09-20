using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using d360.core;
using System.Web.Mvc;
using d360.workflow;
using d360.workflow.entities;

namespace d360.web.Models
{
    public class WorkflowTypeRelationEditorModel
    {
        public WorkflowTypeRelationEditorModel()
        {
            Enabled = true;
            ObjectTypes = new List<SelectListItem>();
            ParentTypes = new List<SelectListItem>();
            ResponsibilityTypes = new List<SelectListItem>();
        }

        public bool Enabled { get; set; }

        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public string FormDescription { get; set; }

        public WorkflowType WorkflowType { get; set; }

        public WorkflowTypeRelation WorkflowTypeRelation { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }

        public List<SelectListItem> ParentTypes { get; set; }

        public List<SelectListItem> ResponsibilityTypes { get; set; }
    }
}