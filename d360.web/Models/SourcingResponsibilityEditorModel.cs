using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class SourcingResponsibilityEditorModel
    {
        public int ID { get; set; }

        public int ResponsibilityTypeID { get; set; }

        public List<EditableFieldItem> Artifacts { get; set; }

        public List<EditableFieldItem> Contexts { get; set; }

        public SystemObjects ObjectType { get; set; }

        public int ObjectID { get; set; }
    }
}