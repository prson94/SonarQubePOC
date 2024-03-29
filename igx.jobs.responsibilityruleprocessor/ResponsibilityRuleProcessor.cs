using AngleSharp.Common;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.responsibilityruleprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class ResponsibilityRuleProcessor : BaseWebJob
	{
		const string FUNCTION_NAME = "ResponsibilityRules_ProcessScheduled";
		const string TIMER_SETTINGS = "0 */3 * * * *";

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;

		public ResponsibilityRuleProcessor(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS, RunOnStartup = true)] TimerInfo myTimer, ILogger log)
		{
			try
			{
				// increase the default dapper timeout from 30 to 90 seconds
				Dapper.SqlMapper.Settings.CommandTimeout = 90;

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
								CompanyPrefix = "",
								ResourceID = 0,
								IsAdministrator = true
							};
							var community = new CommunityContext(ConnString, Cache, Queue, context);
							var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);

							try
							{
								company.ClearInvalidRelationRuleResults();
							}
							catch (Exception dex)
							{
								log.LogError(dex, "Error while clearing relation rules results.");
							}

							try
							{
								await company.ProcessResponsibilityRelationRules();
							}
							catch (Exception ex)
							{
								log.LogError(ex, "Error while processing responsibility rules.");
							}
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error occurred while processing tasks for this environment.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				log.LogCritical(ex, "Critical exception at root of web job");
			}
		}
	}
}
