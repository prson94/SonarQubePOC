using Microsoft.Azure.WebJobs;
using System;

namespace igx.jobs.assetgraphprocessor
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration();

#if DEBUG
            config.UseDevelopmentSettings();
#endif
            config.UseTimers();
            config.UseServiceBus();

            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
