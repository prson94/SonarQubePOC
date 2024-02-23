using AngleSharp.Common;
using d360.extensions;
using d360.extensions.info;
using d360.featureflags;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
	public class WorkflowDigestProcessor: BaseWebJob
	{
		const string FUNCTION_NAME = "Workflow_DigestProcessor";

#if DEBUG
		const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 0 5 * * *"; // every day at 5am
#endif

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly IFeatureFlagService FeatureFlags;

		public WorkflowDigestProcessor(ICachingProvider cache, IConfiguration config, IFeatureFlagService ff, IMailProvider mail, IQueueSource queue): base(config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
		}

		public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log)   
		{
			try
			{
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
							var context = new UriSecurityContextProvider {
								CompanyID = c.CompanyID,
								CompanyPrefix = c.UrlPrefix,
								ResourceID = 0,
								IsAdministrator = true
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
							
							await company.SendDigestEmails(c.EnvironmentLevel);
						}
						catch (Exception ex)
						{
							log.LogError(ex, "Error while processing workflow digests for this environment.");
						}
					}
				}
			}
			catch (Exception ex)
			{
				var logProperties = new Dictionary<string, object> {
					{ "Function", FUNCTION_NAME }
				};

				using (log.BeginScope(logProperties))
				{
					log.LogCritical(ex, "Critical error while running this web job.");
				}
			}
		}
	}
}