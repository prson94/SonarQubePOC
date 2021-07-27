using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddAzureStorage(s => {
#if DEBUG
                    s.MaxDequeueCount = 10;
                    s.BatchSize = 10;
                    s.NewBatchThreshold = 10;
#else
                    s.MaxDequeueCount = 5;
#endif
                    s.VisibilityTimeout = TimeSpan.FromMinutes(30);
                })
                .AddTimers();
            });

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
