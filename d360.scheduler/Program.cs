using Autofac;
using d360.extensions;
using d360.scheduler.jobs;
using Microsoft.WindowsAzure.ServiceRuntime;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Topshelf;

namespace d360.scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
                {
                    x.Service<SchedulingSystem>();
                    x.SetDisplayName("Data3Sixty Scheduling Service");
                    x.RunAsNetworkService();
                    x.SetServiceName("D3S-Scheduling");
                    x.StartAutomatically();
                }
            );
        }
    }

    public class SchedulingSystem : ServiceControl
    {
        ISchedulerFactory schedFact = null;
        IScheduler sched = null;

        void createSchedulingSystemWithJobs()
        {
            // construct a scheduler factory
            schedFact = new StdSchedulerFactory();

            // get a scheduler
            sched = schedFact.GetScheduler();

            #region Refresh job info

            //var jobBuilder1 = JobBuilder.Create(typeof(RefreshWebApplicationJob));
            //var jobDetail1 = jobBuilder1.Build();
            //jobDetail1.JobDataMap.Add("Uri", new List<string> { "https://my.data3sixty.com/refresh/ba123ndhfrktyw" }); //TODO: Generate these from CommunityDB. , "http://bw.data3sixty.com/refresh/ba123ndhfrktyw"
            //var triggerBuilder1 = TriggerBuilder.Create();
            //triggerBuilder1.WithDailyTimeIntervalSchedule(a => { a.OnEveryDay().WithIntervalInMinutes(10); }).StartNow();
            //var trigger1 = triggerBuilder1.Build();

            ////Send the info to the scheduler.
            //sched.ScheduleJob(jobDetail1, trigger1);

            #endregion

            #region Indexer job info

            var jobBuilder2 = JobBuilder.Create(typeof(IndexerJob));
            var jobDetail2 = jobBuilder2.Build();
            var triggerBuilder2 = TriggerBuilder.Create();
            triggerBuilder2.WithDailyTimeIntervalSchedule(a => { a.OnEveryDay().WithIntervalInSeconds(1); }).StartNow();
            var trigger2 = triggerBuilder2.Build();

            //Send the info to the scheduler.
            sched.ScheduleJob(jobDetail2, trigger2);

            #endregion 
        }

        public bool Start(HostControl hostControl)
        {
            try
            {
                if (schedFact == null)
                {
                    createSchedulingSystemWithJobs();
                }
                sched.Start();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Stop(HostControl hostControl)
        {
            try
            {
                sched.Standby();
                return true;            
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
