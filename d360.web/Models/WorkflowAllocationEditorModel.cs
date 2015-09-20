using d360.workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models
{
    public class WorkflowAllocationEditorModel
    {
        public WorkflowAllocationEditorModel()
        {
            Properties = new Dictionary<string, string>();
            Responsibilities = new List<SelectListItem>();
        }

        public WorkflowType WorkflowType { get; set; }

        public string ObjectType { get; set; }

        public int ObjectID { get; set; }

        public bool Enabled { get; set; }

        public bool Required { get; set; }

        public List<SelectListItem> Responsibilities { get; set; }

        public Dictionary<string, string> Properties { get; set; }
    }
}