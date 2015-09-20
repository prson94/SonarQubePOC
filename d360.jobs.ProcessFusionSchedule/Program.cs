using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.core;

namespace d360.jobs.ProcessFusionSchedule
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));
            Functions.CallDatabase();
        }
    }
}
