using d360.core.queue;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using d360.core.enums.Workflow;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Azure.ServiceBus;
using System.Text;

namespace d360.extensions.queue
{
    public class AzureQueueSource : IQueueSource
    {
        public string QueueStorageName { get { return CloudConfigurationManager.GetSetting("QueueStorageName"); } }
        public string QueueStorageKey { get { return CloudConfigurationManager.GetSetting("QueueStorageKey"); } }
        public string EventServiceBusConnectionString { get { return CloudConfigurationManager.GetSetting("EventServiceBus"); } }

        //https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas
        //256KB message size limit, minus 64KB for header
        private const long MAX_MESSAGE_SIZE = (1024 * 256) - (1024 * 64);

        private CloudQueueClient cloudClient {
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

        public bool CreateMessage<T>(string queueName, T item)
        {
            try
            {
                var queueClient = cloudClient;

                var queue = queueClient.GetQueueReference(queueName);

                var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));

                queue.AddMessage(msg, options:queueRequestOptions);

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
                
                await queue.AddMessageAsync(msg,null, initialVisibilityDelay, queueRequestOptions, null);

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
                         queue.AddMessage(msg,initialVisibilityDelay: initialVisibilityDelay);
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
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);

            var client = CreateTopicClient(topicName);
            client.SendAsync(bm).RunSynchronously();
        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);
            var client = CreateTopicClient(topicName);
            client.SendAsync(bm).RunSynchronously();
        }

        public async Task CreateTopicMessageAsync(EventInfo e)
        {
            var topicName = getTopicName();
            await CreateTopicMessageAsync(topicName, e);
        }

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);
            var client = CreateTopicClient(topicName);
            await client.SendAsync(bm);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {
            var topicName = getTopicName();
            CreateTopicMessages(topicName, events);
        }

        public void CreateTopicMessages(string topicName, List<EventInfo> events)
        {
            var batches = new List<List<Message>>();
            long batchSize = 0;

            batches.Add(new List<Message>());

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new Message(eBytes);
                var messageId = $"C{e.CompanyID}_A{e.Action}_W{e.WorkflowItemID}_S{e.VersionStepTransitionID}_I{e.ItemStepID}";

                if (e.Object != null) messageId += $"_O{e.Object.Object}|{e.Object.ObjectID}";
                bm.MessageId = messageId;

                if(e.Action == ChangeType.Add || e.Action == ChangeType.Update) //delay the processing if add or edit so update has chance to process
                    bm.ScheduledEnqueueTimeUtc = DateTime.UtcNow.AddSeconds(15);

                batchSize = AddMessageToBatch(bm, batches, batchSize);

            }

            var client = CreateTopicClient(topicName);

            foreach (var batch in batches)
            {
                client.SendAsync(batch).RunSynchronously();
            }
        }

        public string GetTopicNameBySetting(string settingName)
        {
            return CloudConfigurationManager.GetSetting(settingName);
        }

        private string getTopicName()
        {            
            return (GetTopicNameBySetting("EventBusTopicName") ?? "events-debug");
        }

        private RetryPolicy DefaultTopicRetryPolicy
        {
            get
            {
                return new RetryExponential( // default strategy
                TimeSpan.FromSeconds(0), // default
                TimeSpan.FromSeconds(30), // default
                15); // increased from default of 10
            }
        }

        private TopicClient CreateTopicClient(string topicName)
        {
            var client = new TopicClient(EventServiceBusConnectionString, topicName, DefaultTopicRetryPolicy);            
            return client;
        }

        public async Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            var topicName = getTopicName();
            await CreateTopicMessagesAsync(topicName, events);
        }

        

        public async Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
        {
            var batches = new List<List<Message>>();
            long batchSize = 0;

            batches.Add(new List<Message>());

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new Message(eBytes);

                batchSize = AddMessageToBatch(bm, batches, batchSize);
            }

            var client = CreateTopicClient(topicName);

            foreach (var batch in batches)
            {                
                await client.SendAsync(batch);
            }
        }

        public void CreateTopicMessage<T>(string topicName, T e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);

            var client = CreateTopicClient(topicName);
            client.SendAsync(bm).RunSynchronously();
        }

        public async Task CreateTopicMessageAsync<T>(string topicName, T e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);

            var client = CreateTopicClient(topicName);
            await client.SendAsync(bm);
        }

        public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
        {
            var batches = new List<List<Message>>();
            long batchSize = 0;

            batches.Add(new List<Message>());

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new Message(eBytes);


                if(scheduledEnqueueTime.HasValue)
                {
                    bm.ScheduledEnqueueTimeUtc = scheduledEnqueueTime.Value;
                }

                batchSize = AddMessageToBatch(bm, batches, batchSize);
            }

            var client = CreateTopicClient(topicName);

            foreach (var batch in batches)
            {                
                client.SendAsync(batch).RunSynchronously();
            }

        }

        public async Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
        {
            var batches = new List<List<Message>>();
            long batchSize = 0;

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new Message(eBytes);

                batchSize = AddMessageToBatch(bm, batches, batchSize);
            }

            var client = CreateTopicClient(topicName);

            foreach (var batch in batches)
            {                
                await client.SendAsync(batch);
            }

        }

        private long AddMessageToBatch(Message bm, List<List<Message>> batches, long batchSize)
        {
            batchSize += bm.Size;
            if (batchSize > MAX_MESSAGE_SIZE)
            {
                batchSize = 0;
                batches.Add(new List<Message>());
            }

            batches[batches.Count - 1].Add(bm);

            return batchSize;
        }
    }
}
