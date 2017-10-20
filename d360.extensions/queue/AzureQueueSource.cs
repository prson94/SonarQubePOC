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
using System.Configuration;
using Microsoft.Azure;

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

        public void CreateMessage<T>(string queueName, T item)
        {
            var list = new List<T>() { item };
            CreateMessages(queueName, list);
        }

        public async Task CreateMessageAsync<T>(string queueName, T item)
        {
            var list = new List<T>() { item };
            await CreateMessagesAsync(queueName, list);
        }

        //public void CreateMessage(string queueName, QueueObject item)
        //{
        //    var list = new List<QueueObject>() { item };
        //    CreateMessages(queueName, list);
        //}

        //public void CreateMessages(string queueName, List<QueueObject> items)
        //{
        //    try
        //    {
        //        var queueClient = new CloudQueueClient(
        //            new Uri($"https://{constants.AZURE_STORAGE_NAME}.queue.core.windows.net/"),
        //            getCredentials()
        //        );

        //        var queue = queueClient.GetQueueReference(queueName);

        //        items.ForEach(item =>
        //        {
        //            var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));
        //            queue.AddMessage(msg);
        //        });

        //        queue = null;
        //        queueClient = null;                
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
        //    }
        //}

        public void CreateMessages<T>(string queueName, List<T> items)
        {
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }

        public async Task CreateMessagesAsync<T>(string queueName, List<T> items)
        {
            try
            {
                var queueClient = new CloudQueueClient(
                    new Uri($"https://{constants.AZURE_STORAGE_NAME}.queue.core.windows.net/"),
                    getCredentials()
                );

                var queue = queueClient.GetQueueReference(queueName);

                await Task.Run(() => {
                     items.ForEach(item =>
                     {
                         var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));
                         queue.AddMessage(msg);
                     });
                });

                queue = null;
                queueClient = null;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }

        public void CreateMessage(QueueType type, QueueObject item)
        {
            var list = new List<QueueObject>() { item };
            CreateMessages(type, list);
        }

        public void CreateMessages(QueueType type, List<QueueObject> items)
        {            
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
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }

        public void CreateTopicMessage(EventInfo e)
        {
            var topicName = getTopicName();
            CreateTopicMessageAsync(topicName, e);
        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            bm.Properties["topic"] = topicName;
            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            client.Send(bm);
            client = null;
        }

        public Task CreateTopicMessageAsync(EventInfo e)
        {
            var topicName = getTopicName();
            return CreateTopicMessageAsync(topicName, e);
        }

        public Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            bm.Properties["topic"] = topicName;
            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            return client.SendAsync(bm);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {
            var topicName = getTopicName();
            CreateTopicMessages(topicName, events);
        }

        public void CreateTopicMessages(string topicName, List<EventInfo> events)
        {
            var list = new List<BrokeredMessage>();
            foreach (var e in events)
            {
                var bm = new BrokeredMessage(e);
                var messageId = $"C{e.CompanyID}_A{e.Action}_W{e.WorkflowItemID}_S{e.VersionStepTransitionID}_I{e.ItemStepID}";

                if (e.Object != null) messageId += $"_O{e.Object.Object}|{e.Object.ObjectID}";
                bm.Properties["topic"] = topicName;
                bm.MessageId = messageId;
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            client.SendBatch(list);
            client = null;
        }

        private string getTopicName()
        {            
            return (CloudConfigurationManager.GetSetting("EventBusTopicName") ?? "events-debug");
        }

        public Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            var topicName = getTopicName();
            return CreateTopicMessagesAsync(topicName, events);
        }

        public async Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
        {
            var list = new List<BrokeredMessage>();
            foreach (var e in events)
            {
                var bm = new BrokeredMessage(e);
                bm.Properties["topic"] = topicName;
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            await client.SendBatchAsync(list);
        }
    }
}
