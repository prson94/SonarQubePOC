using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Dapper;
using LaunchDarkly.Logging;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions.consumption
{
    public class DisplayValueChecker: BaseFunction
    {
#if DEBUG
        const string timerSettings = "*/10 * * * * *";
#else
        const string timerSettings = "0 0 */6 * * *"; // every 6 hours
#endif
		ICachingProvider Cache;
		IMailProvider Mail;
		IQueueSource Queue;

		public DisplayValueChecker(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName("DisplayValueChecker")]
        public async Task Run([TimerTrigger(timerSettings)] TimerInfo myTimer, ILogger log)
        {
			var topicName = Config["DisplayValueQueue"];
			var companies = GetCompaniesByCurrentSlot();
			foreach (var c in companies)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", "DatabaseTask_Scheduler" },
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
						var community = new CommunityContext(Cache, Queue, context); ;
						var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);
						var rs = await company.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Active, int.Parse(Config["V2EnvironmentJobRebuildTimeoutInHours"]));
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
