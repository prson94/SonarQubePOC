using d360.scheduler.jobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.extensions;

namespace d360.scheduler
{
    public class JobLoader
    {
        readonly IEnumerable<IScheduledJob> Jobs;

        public JobLoader(IEnumerable<IScheduledJob> jobs)
        {
            Jobs = jobs;
        }

        public void Load(IScheduler sched)
        {
            foreach (var job in Jobs.Where(i => i.Enabled))
            {
                var jobBuilder = JobBuilder.Create(job.GetType());
                var jobDetail = jobBuilder.Build();
                var triggerBuilder = TriggerBuilder.Create();
                //triggerBuilder.WithDailyTimeIntervalSchedule(a => { a.OnEveryDay().WithIntervalInSeconds(1); }).StartNow();
                triggerBuilder.WithDailyTimeIntervalSchedule(a => { a.OnEveryDay().WithIntervalInSeconds(job.IntervalInMinutes); }).StartNow();
                var trigger = triggerBuilder.Build();

                //Send the info to the scheduler.
                sched.ScheduleJob(jobDetail, trigger);
            }
        }
    }
}
