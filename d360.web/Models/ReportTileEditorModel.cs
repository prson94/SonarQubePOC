using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using d360.core.entities;
using d360.core;
using System.Web.Mvc;

namespace d360.web.Models
{
    public class ReportTileEditorModel
    {
        public string FormUri { get; set; }

        public string FormMethod { get; set; }

        public string FormName { get; set; }

        public string FormDirections { get; set; }

        public string ReportBaseUri { get; set; }

        public ReportTile ReportTile { get; set; }

        public List<SelectListItem> ReportTileTypes { get; set; }

        public List<SelectListItem> ContentAreaNumbers { get; set; }

        public List<ReportSchemaModel> SchemaItems { get; set; }

        public List<SelectListItem> ObjectTypes { get; set; }
    }
}