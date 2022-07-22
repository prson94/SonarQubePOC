using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace igx.jobs.bulkloadprocessor
{
    public static class Program
    {
        public static async Task Main()
        {
			var builder = CoreFunction.JobHostConfigBuilder();
			builder.ConfigureWebJobs(c =>
			{
				c.AddTimers();
				c.AddServiceBus(s =>
				{
					s.MessageHandlerOptions.MaxAutoRenewDuration = new TimeSpan(0, 5, 0); // auto renew messages for 5 additional minutes.                    
					s.MessageHandlerOptions.MaxConcurrentCalls = 5; // up to 5 concurrent calls.
				});
			});

			System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
			using (var host = builder.Build())
			{
				await host.RunAsync();
			}
		}
    }
}
