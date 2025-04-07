using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace igx.jobs.indexer
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c =>
				{
					c.AddTimers()
					 .AddAzureStorageQueues();
				})
				.ConfigureGovernLogging();

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
