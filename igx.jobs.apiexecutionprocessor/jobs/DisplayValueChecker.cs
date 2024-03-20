using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using igx.jobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

		public DisplayValueChecker(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
        public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log)
        {
			var topicName = Configuration["DisplayValueQueue"];
			var companies = GetCompaniesByCurrentSlot();
			foreach (var c in companies)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME },
					{ "CompanyID", c.CompanyID },
					{ "UrlPrefix", c.UrlPrefix }
				};

				using (log.BeginScope(logProperties))
				{
					try
					{
						var context = new UriSecurityContextProvider
						{
							CompanyID = c.CompanyID,
							CompanyPrefix = c.UrlPrefix,
							ResourceID = 0,
							IsAdministrator = true,
						};
						var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
						var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true)
						{
							ApiExecutionQueue = Configuration["ApiExecutionQueue"],
							AssetGraphQueue = Configuration["AssetGraphQueue"],
							BulkLoadQueue = Configuration["BulkLoadQueue"],
							DisplayValueQueue = Configuration["DisplayValueQueue"],
							EventBusTopicName = Configuration["EventBusTopicName"],
							ScoringQueue = Configuration["ScoringQueue"],
							SearchIndexQueue = Configuration["SearchIndexQueue"]
						};

						var rs = await company.UpdateRebuildJobStatus(
							CompanyRebuildJobToken.DisplayValues, 
							CompanyRebuildJobStatusState.Active, 
							int.Parse(Configuration["V2EnvironmentJobRebuildTimeoutInHours"])
						);
						if (rs.StatusCode == System.Net.HttpStatusCode.OK)
						{
							await Queue.CreateMessageAsync(topicName, new DisplayUpdateInfo { CompanyID = c.CompanyID, RebuildAll = true });
						}
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Error when rebuilding display values.");
					}
				}
			}
		}
    }
}
