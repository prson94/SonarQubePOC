using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class SourcingResponsibilityTypeEditorModel
    {
        public int ID { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public ResponsibilityTypeGroup ResponsibilityTypeGroup { get; set; }

        public List<EditableFieldItem> ArtifactTypes { get; set; }
    }
}