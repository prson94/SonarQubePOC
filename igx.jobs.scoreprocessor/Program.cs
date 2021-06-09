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
                    s.MaxDequeueCount = 5;
#if DEBUG
                    s.MaxPollingInterval = TimeSpan.FromSeconds(5);
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
