using d360.core;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.workflow
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }


}
