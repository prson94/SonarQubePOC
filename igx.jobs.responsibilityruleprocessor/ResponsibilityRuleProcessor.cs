using d360.extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using System.Threading.Tasks;

namespace igx.jobs.responsibilityruleprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class ResponsibilityRuleProcessor : BaseWebJob
	{
		const string FUNCTION_NAME = "ResponsibilityRules_ProcessScheduled";
		const string TIMER_SETTINGS = "0 */3 * * * *";

		readonly string Region;

		public ResponsibilityRuleProcessor(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(community, config)
		{
			Region = config[constants.Setting.Region];
		}

		[FunctionName(FUNCTION_NAME), Disable]
		public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log)
		{
			//try
			//{
			//	var slot = GetEnvironmentLevelCurrentSlot();
			//	var tenants = (await Community.ReadTenantConnectionSettingsByCurrentSlotAsync(slot, Region)).ToList();

			//	foreach (var c in tenants)
			//	{
			//		var logProperties = new Dictionary<string, object> {
			//			{ "Function", FUNCTION_NAME },
			//			{ "CompanyID", c.CompanyID },
			//			{ "UrlPrefix", c.UrlPrefix }
			//		};

			//		using (log.BeginScope(logProperties))
			//		{
			//			// May end up calling "security.RunRules" new procedure here. Let's see first.
			//		}
			//	}
			//}
			//catch (Exception ex)
			//{
			//	log.LogCritical(ex, "Critical exception at root of web job");
			//}
		}
	}
}
