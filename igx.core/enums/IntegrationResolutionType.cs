using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace d360.core.enums
{
    public enum IntegrationResolutionType
    {
        [Name("Via Owner Identifier")]
        Identifier = 1,
        [Name("Via Steward Relation")]
        StewardRelation = 2
    }
}
