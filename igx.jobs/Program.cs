using ApplicationInsights.Helpers.WebJobs;
using Microsoft.Azure.WebJobs;

namespace igx.jobs
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration {
                DashboardConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                StorageConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                NameResolver = new QueueNameResolver()
            };

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            config.UseApplicationInsights();
            config.UseCore();
            config.UseServiceBus();
            config.UseTimers();

            var host = new JobHost(config);
            host.RunAndBlock();
        }


    }
}
