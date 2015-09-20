using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    public enum StatisticCheckType
    {
        Existence = 1,
        Count = 2,
        PropertyValueCheck = 3,
        PropertyPopulated = 4,
        Relationship = 5,
        FusionOwnership = 6,
        ScoreRollupViaRelationship = 7,
        ScoreRollupViaOwnership = 8,
        EventMetric = 9
    }
}
