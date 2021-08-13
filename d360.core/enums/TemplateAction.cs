using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace d360.core
{
    public enum TemplateAction
    {
        [Description("Group Join Request")]
        JoinRequest,
        [Description("None")]
        None,
        [Description("Preview")]
        Preview,
        [Description("View Statistics")]
        Statistics,
        [Description("Lookup Preview")]
        LookupPreview,
        [Description("Assigning Item Preview")]
        AssigningItemPreview
    }
}
