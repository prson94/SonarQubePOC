using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace igx.jobs.databasecleaner
{
	class Program
    {
        static async Task Main()
        {
			var builder = new HostBuilder();
			builder
				.SetGovernConfiguration()
				.ConfigureWebJobs(c => {
					c.AddTimers();
				})
				.ConfigureGovernLogging();

            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}
