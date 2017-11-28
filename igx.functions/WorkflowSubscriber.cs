using d360.core;
using d360.core.entities.Workflow;
using d360.core.enums.Workflow;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace igx.functions
{
    public static class WorkflowSubscriber
    {
        const string functionName = "WorkflowSubscriber";
        const int MAX_NUMBER_OF_WORKFLOW_EVENTS = 25;

        [FunctionName(functionName)]
        public static async Task Run([ServiceBusTrigger("%EventBusTopicName%", "Workflow", AccessRights.Listen)]BrokeredMessage brokeredMessage, TraceWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);

                log.Info($"WorkflowSubscriber trigger function processed:  {brokeredMessage.MessageId}");

                var info = brokeredMessage.GetBody<EventInfo>();

                #region Create EF connection

                var sec = new UriSecurityContextProvider()
                {
                    CompanyID = info.CompanyID,
                    ResourceID = info.ResourceID,
                    CompanyPrefix = info.DomainPrefix,
                    IsAdministrator = true
                };
                var cache = new DummyCachingProvider();
                var queue = new AzureQueueSource();
                var community = new CommunityContext(cache, queue, sec);
                var company = new CompanyContext(community, cache, queue, sec, true);

                #endregion

                //check if this event already has a open workflow instance
                if (info.WorkflowItemID <= 0)
                {
                    log.Info($"Debug - New [{info.Action}] event received.");

                    var sObject = info.Object.ObjectType.ToString();

                    IQueryable<WorkflowEventRegistration> registrations = null;

                    if (info.Action == ChangeType.Loaded)
                    {
                        sObject = info.Object.Object.ToString();
                        registrations = company.WorkflowEventRegistrations.Where(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectID && i.Type.State == d360.core.enums.State.Active && i.Type.PublishedVersionID != null).OrderBy(x => x.ID).Include(x => x.Type);
                    }
                    else
                    {
                        registrations = company.WorkflowEventRegistrations.Where(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectTypeID && i.Type.State == d360.core.enums.State.Active && i.Type.PublishedVersionID != null).OrderBy(x => x.ID).Include(x => x.Type);
                    }

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
                    log.Info($"Debug - New [{info.Action}] event received.  With an open workflow instance.");

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
                        log.Info($"Debug - Event is a workflow transition.");

                        await company.EvaluateWorkflowTransition(info.VersionStepTransitionID, info.WorkflowItemID, info.Object);
                    }
                    else if (info.ItemStepID > 0) // this event is to evauluate a workflow step
                    {
                        log.Info($"Debug - Event is an item step.");

                        await company.ExecuteStep(info.ItemStepID, info.WorkflowItemID, info.Object);
                    }
                }

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);

                log.Error("Exception: " + ex.GetFullExceptionData());
            }

            CoreFunction.AIFlush();
        }
    }
}
