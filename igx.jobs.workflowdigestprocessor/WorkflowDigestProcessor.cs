using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace igx.jobs.workflowdigestprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
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

		public WorkflowDigestProcessor(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue): base(community, config)
		{
			Community = community;
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[Singleton(Mode = SingletonMode.Function)]
		public async Task Run([TimerTrigger(TIMER_SETTINGS)] TimerInfo myTimer, ILogger log, Microsoft.Azure.WebJobs.ExecutionContext executionContext)   
		{
			//add random delay so instances run at an offset to each other.
			var rand = new Random();
			Thread.Sleep(rand.Next(30) * 1000);

			var slot = GetEnvironmentLevelCurrentSlot();
			var lastestDigestExecutions = (await Community.ReadMostRecentWorkflowDigestStatusBySlotAsync(slot)).ToList();

			await LoopThroughTenantsAsync(log, FUNCTION_NAME, async c => {
				var settings = await Community.ReadSettingsAsync(c.CompanyID);
				int digestDays = int.Parse(settings.Single(o => o.ID == Setting.WorkflowDigestEmailDays).Value);
				var fromEmail = settings.Single(o => o.ID == Setting.WorkflowFromEmail).Value;
				var fromName = settings.Single(o => o.ID == Setting.WorkflowFromName).Value;

				try
				{
					var latestExecution = lastestDigestExecutions.SingleOrDefault(o => o.CompanyID == c.CompanyID);
							
					// Check if digest was already sent today
					bool shouldContinueProcessing = (latestExecution == null) || 
						(latestExecution != null && latestExecution?.LastExecuted?.DayOfWeek != DateTime.UtcNow.DayOfWeek);

					if (shouldContinueProcessing)
					{ 								
						int? id = null;
						if (latestExecution == null)
						{
							id = latestExecution.ID;
						}
						await Community.UpsertWorkflowDigestStatusAsync(c.CompanyID, executionContext.InvocationId, id);

						var context = new UriSecurityContextProvider
						{
							CompanyID = c.CompanyID,
							CompanyPrefix = c.UrlPrefix,
							PrimaryCompanyPrefix = c.UrlPrefix,
							ResourceID = 0,
							IsAdministrator = true
						};
						using (var company = new CompanyContext(Cache, Queue, Mail, context, log, new TenantConnectionInfo { ConnectionString = c.GetConnectionString() } ))
						{
							await company.SendDigestEmails(c.EnvironmentLevel, fromName, fromEmail, digestDays);
						}
					}
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Error while processing workflow digests for this environment.");
				}
			});
		}
	}
}