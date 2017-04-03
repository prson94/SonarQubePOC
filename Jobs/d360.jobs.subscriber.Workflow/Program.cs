using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Dapper;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;
using Microsoft.ServiceBus.Messaging;
using d360.core.queue;
using d360.extensions.info;
using d360.extensions.caching;
using d360.extensions.queue;
using d360.model;
using System.Threading.Tasks;
using d360.extensions;

namespace d360.jobs.subscriber.Workflow
{
    public class Program: FunctionsBase
    {
        public static int MAX_NUMBER_OF_WORKFLOW_EVENTS = 25;

        public static async Task ProcessTopicMessage([ServiceBusTrigger("%topicname%", "Workflow", AccessRights.Listen)] BrokeredMessage message)
        {            
            try
            {
                var info = message.GetBody<EventInfo>();

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
                    Console.WriteLine($"Debug - New {info.Action} event received.");

                    var sObject = info.Object.ObjectType.ToString();
                    var registration = company.WorkflowEventRegistrations.FirstOrDefault(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectTypeID);

                    if (registration != null)
                    {
                        var workflowItem = company.CreateWorkflowItem(registration.TypeID, info.Object, registration, info.ResourceID);
                    }
                }
                else {
                    //load the workflow instance and check how many events have been generated.  if greater than threashold then stop.  Do not raise more events
                    // throw an error this section prevents workflows that go on forever and flood the bus with data...
                    Console.WriteLine($"Debug - New {info.Action} event received.  With an open workflow instance.");

                    var workflowInstance = company.WorkflowItems.Where(x => x.ID == info.WorkflowItemID).FirstOrDefault();

                    if(workflowInstance == null)
                    {
                        throw new Exception("ERROR - CANNOT LOAD SPECIFIED WORKFLOW INSTANCE FROM [WORKFLOW].ITEM TABLE");
                    }

                    if(workflowInstance.NumberOfEvents > MAX_NUMBER_OF_WORKFLOW_EVENTS)
                    {
                        throw new Exception("ERROR - MAX NUMBER OF EVENT BUS EVENTS PER WORKFLOW EXCEEDED!!!");
                    }

                    //increment workflow events and update
                    workflowInstance.NumberOfEvents++;
                    company.SaveChanges();

                    if (info.VersionStepTransitionID > 0)  //this event is to evaluate a workflow transition
                    {
                        Console.WriteLine($"Debug - Event is a workflow transition.");

                        await company.EvaluateWorkflowTransition(info.VersionStepTransitionID, info.WorkflowItemID, info.Object);
                    }
                    else if (info.ItemStepID > 0) // this event is to evauluate a workflow step
                    {
                        Console.WriteLine($"Debug - Event is an item step.");

                        await company.ExecuteStep(info.ItemStepID, info.WorkflowItemID, info.Object);
                    }
                }          
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.GetFullExceptionData());
            }
        }

        static void Main()
        {
            var config = new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION);
            config.UseServiceBus();
            config.NameResolver = new TopicNameResolver();
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
