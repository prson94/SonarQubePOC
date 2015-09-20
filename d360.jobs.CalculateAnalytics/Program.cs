using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.CalculateAnalytics
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));
            Functions.CallDatabase();
        }
    }
}
