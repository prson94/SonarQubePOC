using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using d360.core;
using System.Web.Mvc;

namespace d360.web.Models
{
    public class ReportEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public string FormDirections { get; set; }

        public Report Report { get; set; }

        public List<SelectListItem> ReportLayouts { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }
    }
}