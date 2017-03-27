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

namespace d360.jobs.subscriber.Workflow
{
    public class Program: FunctionsBase
    {
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

                var sObject = info.Object.ObjectType.ToString();
                var registration = company.WorkflowEventRegistrations.FirstOrDefault(i => i.ChangeType == info.Action && i.Object == sObject && i.ObjectID == info.Object.ObjectTypeID);

                if (registration != null)
                {
                    var workflowItem = await company.CreateWorkflowItem(registration.TypeID, info.Object.Object.ToString(), info.Object.ObjectID);
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
