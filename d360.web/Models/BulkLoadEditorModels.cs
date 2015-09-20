using System;
using System.Collections.Generic;
using d360.core;

namespace d360.web.Models
{
    public class LoadTypeRuleEditorModel
    {
        public int? ID { get; set; }
        public int LoadTypeID { get; set; }

        public bool LookupTypeRuleGroupsEnabled { get; set; }
        public List<EditableFieldItem> LookupTypeRuleGroups { get; set; }

        public List<EditableFieldItem> Objects { get; set; }

        public List<EditableFieldItem> Fields { get; set; }
    }
}