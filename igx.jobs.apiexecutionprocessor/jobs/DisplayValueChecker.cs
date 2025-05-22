using d360.core;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using igx.jobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using repositories.azure;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class DisplayValueChecker: BaseWebJob
    {
		const string FUNCTION_NAME = "DisplayValueChecker";

#if DEBUG
		const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 */6 * * *"; // every 6 hours
#endif

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;

		public DisplayValueChecker(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(community, config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
        public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log)
        {
			await LoopThroughTenantsAsync(log, FUNCTION_NAME, async c => {
				var connectionString = CompanyConnectionStringHelper.ConnectionString(c.CompanyID, c.Server, c.Username, c.Password);
				var workspace = new Workspaces(new DapperConnectionProvider { ReadOnlyConnectionString = connectionString, ReadWriteConnectionString = connectionString });

				var context = new UriSecurityContextProvider
				{
					CompanyID = c.CompanyID,
					CompanyPrefix = c.UrlPrefix,
					ResourceID = 0,
					IsAdministrator = true,
				};

				var rs = await workspace.UpsertRebuildStatusAsync(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Active, 12);
				if (rs.IsSuccess)
				{
					await Queue.CreateMessageAsync(constants.Queue.DisplayValue, new DisplayUpdateInfo { CompanyID = c.CompanyID, RebuildAll = true });
				}
			});
		}
    }
}
