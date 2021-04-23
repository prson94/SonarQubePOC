using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace igx.jobs.assetgraphprocessor
{
    class Program
    {
        public static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddTimers();
                c.AddServiceBus();
            });

            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
