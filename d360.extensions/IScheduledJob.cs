using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    public interface IScheduledJob
    {
        int Interval { get; }
        JobIntervalType IntervalType { get;  }
    }
}
