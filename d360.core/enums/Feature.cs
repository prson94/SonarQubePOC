using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    public enum Feature
    {
        [Description("Base feature set.")]
        Base = 0,
        [Description("Events feature set.")]
        Events = 1,
        [Description("Fusion feature set.")]
        Fusion = 2,
        [Description("Social feature set.")]
        Social = 3,
        [Description("Community feature set.")]
        Community = 4,
        [Description("Scoring feature set.")]
        Scoring = 5,
        [Description("Reporting feature set.")]
        Reporting = 6,
        [Description("Survey feature set.")]
        Survey = 7
    }
}
