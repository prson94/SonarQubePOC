using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.core.enums
{
    /// <summary>
    /// USed in the scheduler cloud service when determing how often to execute a scheduled task.
    /// </summary>
    public enum JobIntervalType
    {
        Day = 1,
        Hour = 2,
        Minute = 3,
        Second = 4
    }
}
