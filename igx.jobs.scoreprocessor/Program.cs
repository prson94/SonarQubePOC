using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor
{
    class Program
    {
        static async Task Main()
        {
            using (var host = CoreFunction.JobHostConfigBuilder().Build())
            {
                await host.RunAsync();
            }
        }
    }
}
