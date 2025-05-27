using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.entities.Membership;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace igx.jobs.scheduledworkflowprocessor
{
	[Singleton(Account = "AzureStorageConnectionString", Mode = SingletonMode.Listener)]
	public class ScheduledWorkflowProcessor : BaseWebJob
	{
        const string FUNCTION_NAME = "Workflow_ProcessSchedule";

#if DEBUG
        const string TIMER_SETTINGS = "*/10 * * * * *";
#else
        const string TIMER_SETTINGS = "0 */15 * * * *";
#endif

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;

		public ScheduledWorkflowProcessor(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(community, config)
		{
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

			await LoopThroughTenantsAsync(log, FUNCTION_NAME, async c =>
			{
				var settings = Community.ReadSettingsAsync(c.CompanyID).Result;
				var fromEmail = settings.Single(o => o.ID == Setting.WorkflowFromEmail).Value;
				var fromName = settings.Single(o => o.ID == Setting.WorkflowFromName).Value;

				var context = new UriSecurityContextProvider
				{
					CompanyID = c.CompanyID,
					CompanyPrefix = c.UrlPrefix,
					ResourceID = 0,
					IsAdministrator = true
				};
				using (var company = new CompanyContext(Cache, Queue, Mail, context, log, new TenantConnectionInfo { ConnectionString = c.GetConnectionString() }))
				{
					// Load all workflows of type schedule.
					var scheduledWorkflows = company.WorkflowEventRegistrations.Where(x => x.ChangeType == ChangeType.Schedule && x.Type.State == State.Active && x.Type.PublishedVersionID != null).Include(x => x.Type).ToList();

					foreach (var registration in scheduledWorkflows)
					{
						// If the registration applies fire of the workflow and break if not go to the next one.
						if (company.ExecuteScheduledWorkflow(registration, executionContext.InvocationId, fromName, fromEmail).Result)
						{
							break;
						}
					}

					company.ExecuteTimerSteps();
				}
			});
		}
    }
}