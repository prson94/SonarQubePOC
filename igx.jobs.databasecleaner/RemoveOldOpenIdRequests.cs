using d360.core.enums;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using repositories;

namespace igx.jobs.databasecleaner
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class RemoveOldOpenIdRequests : BaseWebJob
	{
		const string FUNCTION_NAME = "RemoveOldOpenIdRequests";
        const string TIMER_SETTINGS = "0 */30 * * * *";

		public RemoveOldOpenIdRequests(IConfiguration config, ICommunity community) : base(community, config) { }

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME }
				};
			using (log.BeginScope(logProperties))
			{
				try
				{
					await Community.RemoveOldOpenIdRequestsAsync();
					log.LogInformation("Cleared out old OpenIdRequests");
				}
				catch (Exception ex)
				{
					log.LogCritical(ex, "Web job failed.");
				}
			}
		}
	}
}
