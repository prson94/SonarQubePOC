using d360.core.entities.Workflow;
using d360.core.entities.Membership;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using repositories;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
	public class WorkflowQueueListener : BaseWebJob
	{
		const string FUNCTION_NAME = "Workflow_Queue_Listener";

		const int MAX_NUMBER_OF_WORKFLOW_EVENTS = 10000;

		internal readonly ICachingProvider Cache;
		internal readonly IMailProvider Mail;
		internal readonly IQueueSource Queue;

		public WorkflowQueueListener(IConfiguration config, ICommunity community, ICachingProvider cache, IMailProvider mail, IQueueSource queue) : base(community, config)
		{
			Cache = cache;
			Mail = mail;
			Queue = queue;
		}

		[FunctionName(FUNCTION_NAME)]
		public async Task Listen([QueueTrigger(constants.Queue.Workflow, Connection = constants.Setting.Storage)] string myQueueItem, ILogger log)
		{
			var info = JsonConvert.DeserializeObject<EventInfo>(myQueueItem);
			await ProcessMessage(FUNCTION_NAME, info, log);
		}

		public async Task ProcessMessage(string functionName, EventInfo info, ILogger log)
		{
			var companyId = 0;

			var logProperties = new Dictionary<string, object> {
				{ "Function", functionName },
				{ "CompanyID", info.CompanyID },
				{ "UrlPrefix", info.DomainPrefix },
				{ "WorkflowItemId", info.WorkflowItemID },
				{ "VersionStepTransitionId", info.VersionStepTransitionID },
				{ "WorkflowAction", info.Action.ToString() }
			};

			using (log.BeginScope(logProperties))
			{
				companyId = info.CompanyID;
				var context = new UriSecurityContextProvider
				{
					CompanyID = companyId,
					CompanyPrefix = info.DomainPrefix,
					PrimaryCompanyPrefix = info.DomainPrefix,
					ResourceID = info.ResourceID,
					IsAdministrator = true
				};
				string connectionString = Community.GetConnectionStringForTenant(companyId);
				using (var company = new CompanyContext(Cache, Queue, Mail, context, log, new TenantConnectionInfo { ConnectionString = connectionString }))
				{
					try
					{
						//check if this event already has a open workflow instance
						if (info.WorkflowItemID <= 0)
						{
							log.LogTrace($"Debug - New [{info.Action}] event received.");

							var sObject = info.Object.ObjectType.ToString();

							List<WorkflowEventRegistration> registrations = null;

							registrations = company.WorkflowEventRegistrations.Where(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectTypeID && i.Type.State == d360.core.enums.State.Active && i.Type.PublishedVersionID != null).OrderBy(x => x.ID).Include(x => x.Type).ToList();

							if (registrations == null)
							{
								return;
							}

							foreach (var registration in registrations)
							{
								// if the registration applies fire of the workflow and break if not go to the next one.
								await company.CreateWorkflowItem(registration.TypeID, info.Object, registration, info.ResourceID);
							}
						}
						else
						{
							//load the workflow instance and check how many events have been generated.  if greater than threashold then stop.  Do not raise more events
							// throw an error this section prevents workflows that go on forever and flood the bus with data...
							log.LogTrace($"Debug - New [{info.Action}] event received.  With an open workflow instance.");

							var workflowInstance = company.WorkflowItems.Where(x => x.ID == info.WorkflowItemID).FirstOrDefault();

							if (workflowInstance == null)
							{
								throw new MissingRecordException("workflow.Item", info.WorkflowItemID.ToString(), "CANNOT LOAD SPECIFIED WORKFLOW INSTANCE");
							}

							if (workflowInstance.NumberOfEvents > MAX_NUMBER_OF_WORKFLOW_EVENTS)
							{
								throw new InfrastructureException("MAX NUMBER OF EVENT BUS EVENTS PER WORKFLOW EXCEEDED.", "Workflow Service Bus");
							}

							//increment workflow events and update
							workflowInstance.NumberOfEvents++;
							company.SaveChanges();

							if (info.VersionStepTransitionID > 0)  //this event is to evaluate a workflow transition
							{
								log.LogTrace($"Debug - Event is a workflow transition.");

								await company.EvaluateWorkflowTransition(info.VersionStepTransitionID, info.WorkflowItemID, info.Object);
							}
							else if (info.ItemStepID > 0) // this event is to evauluate a workflow step
							{
								log.LogTrace($"Debug - Event is an item step.");

								var settings = await Community.ReadSettingsAsync(companyId);
								var defaultGroup = settings.Single(o => o.ID == d360.core.enums.Setting.WorkflowCatchAllGroup).Value;
								var fromEmail = settings.Single(o => o.ID == d360.core.enums.Setting.WorkflowFromEmail).Value;
								var fromName = settings.Single(o => o.ID == d360.core.enums.Setting.WorkflowFromName).Value;
								await company.ExecuteStep(info.ItemStepID, info.WorkflowItemID, info, defaultGroup, fromName, fromEmail);
							}
						}
					}
					catch (Exception ex)
					{
						log.LogError(ex, "Error while processing workflow activity.");
					}
				}
			}
		}
	}
}
