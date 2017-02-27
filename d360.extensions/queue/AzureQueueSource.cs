using d360.core.queue;
using Microsoft.ServiceBus.Messaging;
using d360.core.enums;
using d360.core;
using System;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using d360.core.enums.Workflow;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
    public class AzureQueueSource : IQueueSource
    {
        private StorageCredentials getCredentials()
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            return new StorageCredentials(acctName, keyValue);
        }

        public void CreateMessage(QueueType type, QueueObject item)
        {
            var list = new List<QueueObject>() { item };
            CreateMessages(type, list);
        }

        public void CreateMessages(QueueType type, List<QueueObject> items)
        {
            //var connectionString = constants.SERVICE_BUS_ACTIONS;
            var queueName = "";

            switch (type)
            {
                case QueueType.BulkLoad:
                    queueName = "d3s-bulkload";
                    break;
                case QueueType.BulkLoadDev:
                    queueName = "d3s-bulkload-debug";
                    break;
                case QueueType.Events:
                    queueName = "d3s-events";
                    break;
                case QueueType.EventsDev:
                    queueName = "d3s-events-debug";
                    break;
                case QueueType.CommunityAction:
                    queueName = "community-actions";
                    break;
                case QueueType.CommunityProcess:
                    queueName = "community-processes";
                    break;
                case QueueType.CompanyAction:
                    queueName = "d3s-actions";
                    break;
                case QueueType.CompanyProcess:
                    queueName = "company-processes";
                    break;
            }

            try
            {
                var queueClient = new CloudQueueClient(
                    new Uri($"https://{constants.AZURE_STORAGE_NAME}.queue.core.windows.net/"),
                    getCredentials()
                );

                var queue = queueClient.GetQueueReference(queueName);

                items.ForEach(item =>
                {
                    var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));
                    queue.AddMessage(msg);
                });

                queue = null;
                queueClient = null;

                //var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
                //var m = new BrokeredMessage(item);
                //m.To = item.To.ToString();
                //queueClient.Send(m);
                //queueClient = null;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }

        public void CreateTopicMessage(EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, "Events"); //Microsoft.ServiceBus.ConnectionString in app.config
            client.Send(bm);
            client = null;
        }

        public Task CreateTopicMessageAsync(EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, "Events"); //Microsoft.ServiceBus.ConnectionString in app.config
            return client.SendAsync(bm);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {
            var list = new List<BrokeredMessage>();
            foreach (var e in events)
            {
                var bm = new BrokeredMessage(e);
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, "Events"); //Microsoft.ServiceBus.ConnectionString in app.config
            client.SendBatch(list);
            client = null;
        }

        public Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            var list = new List<BrokeredMessage>();
            foreach (var e in events)
            {
                var bm = new BrokeredMessage(e);
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, "Events"); //Microsoft.ServiceBus.ConnectionString in app.config
            return client.SendBatchAsync(list);
        }
    }
}
