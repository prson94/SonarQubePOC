using Microsoft.Azure.WebJobs;
using d360.core;

namespace d360.jobs.FusionPromotion
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
