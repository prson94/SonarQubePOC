using System;
using d360.extensions;

namespace d360.scheduler.jobs
{
    public abstract class BaseJob: IDisposable, IScheduledJob
    {
        public virtual int IntervalInMinutes { get; set; }
    }
}
