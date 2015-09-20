using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.extensions;
using System.Configuration;

namespace d360.scheduler.jobs
{
    [DisallowConcurrentExecution()]
    public class BuildDynamicReportsJob: BaseJob, IJob, IScheduledJob
    {
        public override bool Enabled { get { return GetEnabledFromConfig(this.GetType().Name); } }
        public override int IntervalInMinutes { get { return GetIntervalFromConfig(this.GetType().Name); } }
        public override string JobName { get { return "Dynamic Report Builder"; } }

        public void Execute(IJobExecutionContext context)
        {
            //throw new NotImplementedException();
        }
    }
}
