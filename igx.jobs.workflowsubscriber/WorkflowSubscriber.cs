using Azure.Messaging.ServiceBus;
using d360.core.entities.Workflow;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.extensions.info;
using d360.featureflags;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
	public class WorkflowSubscriber : BaseWebJob
	{
		const string FUNCTION_NAME = "Workflow_Subscriber";
		const int MAX_NUMBER_OF_WORKFLOW_EVENTS = 10000;

		readonly ICachingProvider Cache;
		readonly IMailProvider Mail;
		readonly IQueueSource Queue;
		readonly IFeatureFlagService FeatureFlags;

		public WorkflowSubscriber(IConfiguration config, ICachingProvider cache, IMailProvider mail, IQueueSource queue, IFeatureFlagService ff) : base(config)
		{
			Cache = cache;
			FeatureFlags = ff;
			Mail = mail;
			Queue = queue;
		}

		public async Task Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow", Connection = "EventServiceBus")] ServiceBusReceivedMessage brokeredMessage, ILogger log)
		{
			string messageString;
			EventInfo info;
			var companyId = 0;

			try
			{
				messageString = Encoding.UTF8.GetString(brokeredMessage.Body.ToArray());
				info = JsonConvert.DeserializeObject<EventInfo>(messageString);
			}
			catch (Exception ex)
			{
				log.LogError(ex, "Cannot convert workflow payload to EventInfo type.");
				return;
			}

			var logProperties = new Dictionary<string, object> {
				{ "Function", FUNCTION_NAME },
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
					ResourceID = info.ResourceID,
					IsAdministrator = true
				};
				var community = new CommunityContext(Configuration["CommunityContext"], Cache, Queue, context);
				var company = new CompanyContext(community, Cache, Queue, Mail, context, log, true);

				try
				{
					company.FeatureFlags_TEMP_ASSIGNMENTS = FeatureFlags.IsThisTrue(FlagList.TEMP_ASSIGNMENTS, company.GetFeatureFlagUser());

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

							await company.ExecuteStep(info.ItemStepID, info.WorkflowItemID, info);
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
