using d360.core.queue;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using d360.core.enums.Workflow;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.RetryPolicies;
using System.Text;
using Azure.Messaging.ServiceBus;
using System.Collections.Concurrent;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace d360.extensions.queue
{
    public class AzureQueueSource : IQueueSource
    {
        private string queueStorageName;
        private string queueStorageKey;
        private string eventServiceBusConnectionString;
        private readonly string eventBusTopicName;

        public string QueueStorageName
        {
            get
            {
                if (string.IsNullOrEmpty(queueStorageName))
                {
                    queueStorageName = ConfigurationManager.AppSettings["QueueStorageName"];
                }

                return queueStorageName;
            }
        }

        public string QueueStorageKey
        {
            get
            {
                if (string.IsNullOrEmpty(queueStorageName))
                {
                    queueStorageKey = ConfigurationManager.AppSettings["QueueStorageKey"];
                }

                return queueStorageKey;
            }
        }

        public string EventServiceBusConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(eventServiceBusConnectionString))
                {
                    eventServiceBusConnectionString = ConfigurationManager.AppSettings["EventServiceBus"];
                }

                return eventServiceBusConnectionString;
            }
        }

        //keep service bus clients and senders static and reusable where possible
        //these clients are thread safe and designed to be used with DI or singleton patterns
        private static ServiceBusClient ServiceBusClient;
        private static ConcurrentDictionary<string, ServiceBusSender> ServiceBusSenders;

        public AzureQueueSource()
        {

        }

        public AzureQueueSource(IConfiguration config)
        {
            queueStorageName = config["QueueStorageName"];
            queueStorageKey = config["QueueStorageKey"];
            eventServiceBusConnectionString = config["EventServiceBus"];
            eventBusTopicName = config["EventBusTopicName"];
        }

        private CloudQueueClient cloudClient
        {
            get
            {
                return new CloudQueueClient(
                    new Uri($"https://{QueueStorageName}.queue.core.windows.net/"),
                    getCredentials()
                );
            }
        }

        private StorageCredentials getCredentials()
        {
            return new StorageCredentials(QueueStorageName, QueueStorageKey);
        }

        private QueueRequestOptions queueRequestOptions
        {
            get
            {
                var expRetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2), 3);

                return new QueueRequestOptions { RetryPolicy = expRetryPolicy };
            }
        }

        private ServiceBusMessage GetFilteredServiceBusMessage(IFilteredServiceBusMessage o)
        {
            var bm = GetServiceBusMessageFromObject(o);
            
            if (!string.IsNullOrEmpty(o.EventType))
            {
                bm.ApplicationProperties.Add("EventType", o.EventType);
            }

            return bm;
        }

        private ServiceBusMessage GetServiceBusMessageFromObject(object o)
        {
            var eString = JsonConvert.SerializeObject(o);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new ServiceBusMessage(new BinaryData(eBytes));
            bm.MessageId = Guid.NewGuid().ToString();

            return bm;
        }

        public string GetMessageIdFromEventInfo(EventInfo eventInfo)
        {
            if (eventInfo == null)
            {
                throw new ArgumentNullException("eventInfo");
            }

            string messageId = $"C{eventInfo.CompanyID}_A{eventInfo.Action}_W{eventInfo.WorkflowItemID}_S{eventInfo.VersionStepTransitionID}_I{eventInfo.ItemStepID}";

            if (eventInfo.Object != null)
            {
                messageId += $"_O{eventInfo.Object.Object}|{eventInfo.Object.ObjectID}";
            }

            return messageId;
        }

        public bool CreateMessage<T>(string queueName, T item)
        {
            try
            {
                var queueClient = cloudClient;

                var queue = queueClient.GetQueueReference(queueName);

                var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));

                queue.AddMessage(msg, options: queueRequestOptions);

                // per azure docs popreceipt should be present if sucess https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.queue.cloudqueue.addmessage?view=azure-dotnet-legacy
                // check added to ensure message was delivered.
                if (string.IsNullOrEmpty(msg.PopReceipt))
                    throw new Exception("Queue message has no population receipt and appears to not have been added properly");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public async Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
            try
            {
                var queueClient = cloudClient;

                var queue = queueClient.GetQueueReference(queueName);

                var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));

                await queue.AddMessageAsync(msg, null, initialVisibilityDelay, queueRequestOptions, null);

                // per azure docs popreceipt should be present if sucess https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.storage.queue.cloudqueue.addmessage?view=azure-dotnet-legacy
                // check added to ensure message was delivered.
                if (string.IsNullOrEmpty(msg.PopReceipt))
                    throw new Exception("Queue message has no population receipt and appears to not have been added properly");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public bool CreateMessages<T>(string queueName, List<T> items)
        {
            try
            {
                var queueClient = cloudClient;

                var queue = queueClient.GetQueueReference(queueName);

                items.ForEach(item =>
                {
                    var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));

                    queue.AddMessage(msg);
                });

            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public async Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            try
            {
                var queueClient = cloudClient;

                var queue = queueClient.GetQueueReference(queueName);

                await Task.Run(() => {
                    items.ForEach(item =>
                    {
                        var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));
                        queue.AddMessage(msg, initialVisibilityDelay: initialVisibilityDelay);
                    });
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
                return false;
            }
            return true;
        }

        public void CreateTopicMessage(EventInfo e)
        {
            var topicName = getTopicName();
            var bm = GetServiceBusMessageFromObject(e);

            var sender = CreateServiceBusSender(topicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public async Task CreateTopicMessageAsync(EventInfo e)
        {
            var topicName = getTopicName();
            await CreateTopicMessageAsync(topicName, e);
        }

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public async Task CreateScheduledTopicMessageAsync(EventInfo e, DateTimeOffset delay)
        {
            var topicName = getTopicName();
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.ScheduleMessageAsync(bm, delay);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {
            var topicName = getTopicName();
            CreateTopicMessages(topicName, events);
        }

        public void CreateTopicMessages(string topicName, List<EventInfo> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var e in events)
            {
                var msg = GetServiceBusMessageFromObject(e);
                messages.Enqueue(msg);
                msg.MessageId = GetMessageIdFromEventInfo(e);

                if (e.Action == ChangeType.Add || e.Action == ChangeType.Update) //delay the processing if add or edit so update has chance to process
                    msg.ScheduledEnqueueTime = DateTime.UtcNow.AddSeconds(15);
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = sender.CreateMessageBatchAsync().Result)
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    sender.SendMessagesAsync(batch).Wait();
                }
            }
        }

        public string GetTopicNameBySetting(string settingName)
        {
            return ConfigurationManager.AppSettings[settingName];
        }

        private string getTopicName()
        {
            if (!string.IsNullOrEmpty(eventBusTopicName))
            {
                return eventBusTopicName;
            }

            return (GetTopicNameBySetting("EventBusTopicName") ?? "events-debug");
        }

        private ServiceBusSender CreateServiceBusSender(string topicName)
        {
            if (ServiceBusClient == null)
            {
                ServiceBusClient = new ServiceBusClient(EventServiceBusConnectionString);
            }

            if (ServiceBusSenders == null)
            {
                ServiceBusSenders = new ConcurrentDictionary<string, ServiceBusSender>();
            }

            if (!ServiceBusSenders.ContainsKey(topicName))
            {
                ServiceBusSenders.TryAdd(topicName, ServiceBusClient.CreateSender(topicName));
            }

            return ServiceBusSenders[topicName];
        }

        public async Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            var topicName = getTopicName();
            await CreateTopicMessagesAsync(topicName, events);
        }

        public async Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                messages.Enqueue(GetServiceBusMessageFromObject(@event));
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync())
                {
                    
                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    await sender.SendMessagesAsync(batch);
                }
            }
        }

        public void CreateTopicMessage<T>(string topicName, T e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            sender.SendMessageAsync(bm).Wait();
        }

        public async Task CreateTopicMessageAsync<T>(string topicName, T e)
        {
            var bm = GetServiceBusMessageFromObject(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public async Task CreateFilteredTopicMessageAsync(string topicName, IFilteredServiceBusMessage e)
        {
            var bm = GetFilteredServiceBusMessage(e);
            var sender = CreateServiceBusSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                var msg = GetServiceBusMessageFromObject(@event);
                if (scheduledEnqueueTime.HasValue)
                {
                    msg.ScheduledEnqueueTime = scheduledEnqueueTime.Value;
                }
                messages.Enqueue(msg);
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = sender.CreateMessageBatchAsync().Result)
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    sender.SendMessagesAsync(batch).Wait();
                }
            }
        }

        public async Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
        {
            var sender = CreateServiceBusSender(topicName);
            var messages = new Queue<ServiceBusMessage>();

            foreach (var @event in events)
            {
                messages.Enqueue(GetServiceBusMessageFromObject(@event));
            }

            while (messages.Count > 0)
            {
                var partitionKey = Guid.NewGuid().ToString();
                using (ServiceBusMessageBatch batch = await sender.CreateMessageBatchAsync())
                {

                    while (messages.Count > 0)
                    {
                        var msg = messages.Peek();
                        msg.PartitionKey = partitionKey;
                        if (batch.TryAddMessage(msg))
                        {
                            messages.Dequeue();
                        }
                        else
                        {
                            break;
                        }
                    }

                    await sender.SendMessagesAsync(batch);
                }
            }
        }
    }
}
