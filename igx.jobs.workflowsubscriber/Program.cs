using d360.core;
using d360.core.entities.Workflow;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.mail;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceBus.Messaging;
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


            using (var host = builder.Build())
            {
                await host.RunAsync();
            }
        }
    }

    public class WorkflowSubscriber
    {
        const string functionName = "Workflow_Subscriber";
        const int MAX_NUMBER_OF_WORKFLOW_EVENTS = 10000;

        public static async Task Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow")]Message brokeredMessage, TextWriter log)
        {
            string messageString;
            EventInfo info;
            CompanyContext company;
            var companyId = 0;

            CoreFunction.AITrackJobStart(functionName);
            log.WriteLine($"WorkflowSubscriber trigger function processed:  {brokeredMessage.MessageId}");
            CoreFunction.AITrackEvent(functionName, "WorkflowSubscriber triggered", new Dictionary<string, string> { { "MessageID", brokeredMessage.MessageId } });

            try
            {
                messageString = Encoding.UTF8.GetString(brokeredMessage.Body);
                info = JsonConvert.DeserializeObject<EventInfo>(messageString);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, companyId);
                log.WriteLine("Exception: " + ex.GetFullExceptionData());
                return;
            }

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
                //check if this event already has a open workflow instance
                if (info.WorkflowItemID <= 0)
                {
                    log.WriteLine($"Debug - New [{info.Action}] event received.");
                    CoreFunction.AITrackEvent(functionName, "WorkflowSubscriber starting new workflow instance", new Dictionary<string, string> { { "CompanyID", info.CompanyID.ToString() }, { "Action", info.Action.ToString() } });

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
                    log.WriteLine($"Debug - New [{info.Action}] event received.  With an open workflow instance.");
                    CoreFunction.AITrackEvent(functionName, "WorkflowSubscriber continuing existing workflow instance", new Dictionary<string, string> { { "CompanyID", info.CompanyID.ToString() }, { "Action", info.Action.ToString() }, { "WorkflowItemID", info.WorkflowItemID.ToString() } });

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
                        log.WriteLine($"Debug - Event is a workflow transition.");
                        CoreFunction.AITrackEvent(functionName, "WorkflowSubscriber starting new transition", new Dictionary<string, string> { { "CompanyID", info.CompanyID.ToString() }, { "Action", info.Action.ToString() }, { "WorkflowItemID", info.WorkflowItemID.ToString() }, { "VersionStepTransitionID", info.VersionStepTransitionID.ToString() } });

                        await company.EvaluateWorkflowTransition(info.VersionStepTransitionID, info.WorkflowItemID, info.Object);
                    }
                    else if (info.ItemStepID > 0) // this event is to evauluate a workflow step
                    {
                        log.WriteLine($"Debug - Event is an item step.");
                        CoreFunction.AITrackEvent(functionName, "WorkflowSubscriber starting new transition", new Dictionary<string, string> { { "CompanyID", info.CompanyID.ToString() }, { "Action", info.Action.ToString() }, { "WorkflowItemID", info.WorkflowItemID.ToString() }, { "ItemStepID", info.ItemStepID.ToString() } });

                        await company.ExecuteStep(info.ItemStepID, info.WorkflowItemID, info);
                    }
                }

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, companyId);
                log.WriteLine("Exception: " + ex.GetFullExceptionData());
            }

            CoreFunction.AIFlush();
        }
    }
}
