using d360.core;
using d360.core.entities.Workflow;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using LaunchDarkly.Sdk.Server;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.workflowsubscriber
{
    class Program
    {
        static async Task Main()
        {
            var builder = CoreFunction.JobHostConfigBuilder();
            builder.ConfigureWebJobs(c =>
            {
                c.AddAzureStorageCoreServices()
                .AddServiceBus(s =>
                {
                    s.MessageHandlerOptions.MaxAutoRenewDuration = new TimeSpan(0, 5, 0); // auto renew messages for 5 additional minutes.                    
                    s.MessageHandlerOptions.MaxConcurrentCalls = 25; // up to 25 concurrent calls.
                })
                .AddAzureStorage()
                .AddTimers()
                .AddFiles();
            });

			builder.ConfigureServices(services =>
			{
				services.AddSingleton<LaunchDarkly.Sdk.Server.LdClient>(x =>
				{
					return ActivatorUtilities.CreateInstance<LaunchDarkly.Sdk.Server.LdClient>(x, Config.GetValue<string>("LaunchDarklySdkKey"));
				});
			});

			using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class WorkflowSubscriber
    {
		readonly LaunchDarkly.Sdk.Server.LdClient LdClient;
		const string FUNCTION_NAME = "Workflow_Subscriber";
        const int MAX_NUMBER_OF_WORKFLOW_EVENTS = 10000;

		public WorkflowSubscriber(LaunchDarkly.Sdk.Server.LdClient ldc)
		{
			this.LdClient = ldc;
		}

		public async Task Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow")]Message brokeredMessage, ILogger log)
        {
            string messageString;
            EventInfo info;
            CompanyContext company;
            var companyId = 0;

            try
            {
                messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
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
				// Create EF connection
				companyId = info.CompanyID;
				company = JobDbContextCreator.CreateCompanyContext(
					new UriSecurityContextProvider
					{
						CompanyID = companyId,
						CompanyPrefix = info.DomainPrefix,
						ResourceID = info.ResourceID,
						IsAdministrator = true
					},
					new MandrillMailProvider
					{
						ApiKey = ConfigurationManager.AppSettings[constants.MAIL_API_KEY],
						SubAccount = ConfigurationManager.AppSettings[constants.MAIL_SUB_ACCOUNT]
					},
					new AzureQueueSource(),
					new DummyCachingProvider(),
					constants.COMMUNITY_DATABASE_CONNECTION);

				try
				{
					company.FeatureFlags_TEMP_ASSIGNMENTS = LdClient.BoolVariation(FeatureFlags.TEMP_ASSIGNMENTS, company.GetSdkFeatureFlagUser(), false);

					//check if this event already has a open workflow instance
					if (info.WorkflowItemID <= 0)
					{
						log.LogTrace($"Debug - New [{info.Action}] event received.");

						var sObject = info.Object.ObjectType.ToString();

						List<WorkflowEventRegistration> registrations = null;

						registrations = company.WorkflowEventRegistrations.Where(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectTypeID && i.Type.State == d360.core.enums.State.Active && i.Type.PublishedVersionID != null).OrderBy(x => x.ID).Include(x => x.Type).ToList();

						if (registrations == null) return;

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
							throw new Exception("ERROR - CANNOT LOAD SPECIFIED WORKFLOW INSTANCE FROM [WORKFLOW].ITEM TABLE");
						}

						if (workflowInstance.NumberOfEvents > MAX_NUMBER_OF_WORKFLOW_EVENTS)
						{
							throw new Exception("ERROR - MAX NUMBER OF EVENT BUS EVENTS PER WORKFLOW EXCEEDED!!!");
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
