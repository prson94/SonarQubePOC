using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace d360.core.enums
{
    public enum ApiUriType
    {
        [
            Name("Collection"), 
            Description("Collection.")
        ]
        Collection = 1,
        [
            Name("Singleton"),
            Description("Single asset.")
        ]
        Singleton = 2
    }
}
