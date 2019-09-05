using Microsoft.Azure.WebJobs;

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

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
