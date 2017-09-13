using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    public enum FusionRuleType
    {
        [Name("Promote")]
        Promote,
        [Name("Find")]
        Find,
        [Name("Relate")]
        Relate,
        [Name("Lineage")]
        Lineage,
        [Name("FindRelation")]
        FindRelation,
        [Name("Update")]
        Update  
    }
}
