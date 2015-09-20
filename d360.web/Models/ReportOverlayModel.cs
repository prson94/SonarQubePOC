using d360.core.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace d360.web.Models
{
    public class ReportOverlayModel: ObjectModel
    {
        public int ReportID { get; set; }
        public string ReportName { get; set; }
        public List<SelectListItem> ObjectTypes { get; set; }
    }
}