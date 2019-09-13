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
        public string QueueStorageName { get { return CloudConfigurationManager.GetSetting("QueueStorageName"); } }
        public string QueueStorageKey { get { return CloudConfigurationManager.GetSetting("QueueStorageKey"); } }
        public string EventServiceBusConnectionString { get { return CloudConfigurationManager.GetSetting("EventServiceBus"); } }

        private StorageCredentials getCredentials()
        {
            return new StorageCredentials(QueueStorageName, QueueStorageKey);
        }

        public void CreateMessage<T>(string queueName, T item)
        {
            var list = new List<T>() { item };
            CreateMessages(queueName, list);
        }

        public async Task CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
            var list = new List<T>() { item };
            await CreateMessagesAsync(queueName, list, initialVisibilityDelay);
        }

        public void CreateMessages<T>(string queueName, List<T> items)
        {
            try
            {
                var queueClient = new CloudQueueClient(
                    new Uri($"https://{QueueStorageName}.queue.core.windows.net/"),
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

        public async Task CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            try
            {
                var queueClient = new CloudQueueClient(
                    new Uri($"https://{QueueStorageName}.queue.core.windows.net/"),
                    getCredentials()
                );

                var queue = queueClient.GetQueueReference(queueName);

                await Task.Run(() => {
                     items.ForEach(item =>
                     {
                         var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));                         
                         queue.AddMessage(msg,initialVisibilityDelay: initialVisibilityDelay);
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

        public void CreateTopicMessage(EventInfo e)
        {
            var topicName = getTopicName();
            var bm = new BrokeredMessage(e);
            bm.Properties["topic"] = topicName;
            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName);
            client.Send(bm);
        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            bm.Properties["topic"] = topicName;
            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName);
            client.Send(bm);            
        }

        public async Task CreateTopicMessageAsync(EventInfo e)
        {
            var topicName = getTopicName();
            await CreateTopicMessageAsync(topicName, e);
        }

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var bm = new BrokeredMessage(e);
            bm.Properties["topic"] = topicName;
            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName);
            await client.SendAsync(bm);
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

                if(e.Action == ChangeType.Add || e.Action == ChangeType.Update) //delay the processing if add or edit so update has chance to process
                    bm.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(15);
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            client.SendBatch(list);            
        }

        public string GetTopicNameBySetting(string settingName)
        {
            return CloudConfigurationManager.GetSetting(settingName);
        }

        private string getTopicName()
        {            
            return (GetTopicNameBySetting("EventBusTopicName") ?? "events-debug");
        }

        public async Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            var topicName = getTopicName();
            await CreateTopicMessagesAsync(topicName, events);
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

            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
            await client.SendBatchAsync(list);
        }
    }
}
