using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading.Tasks;

using Azure.Messaging.ServiceBus;

using d360.core.queue;

using Newtonsoft.Json;

namespace d360.extensions.queue
{
    public class DummyQueueSource : IQueueSource
    {
        public string EventServiceBusConnectionString => ConfigurationManager.AppSettings["EventServiceBus"];

        public bool CreateMessage<T>(string queueName, T item)
        {
            return true;
        }

        public bool CreateMessages<T>(string queueName, List<T> items)
        {
            return true;
        }

        public async Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
            await Task.Run(() => { });
            return true;
        }

        public async Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            await Task.Run(() => { });
            return true;
        }

        public string GetMessageIdFromEventInfo(EventInfo eventInfo)
        {
            return string.Empty;
        }

        public void CreateTopicMessage(EventInfo e)
        {

        }

        public void CreateTopicMessage(string topicName, EventInfo e)
        {

        }

        public async Task CreateTopicMessageAsync(EventInfo e)
        {
            await CreateTopicMessageAsync("events", e);
        }

        public async Task CreateScheduledTopicMessageAsync(EventInfo e, DateTimeOffset delay)
        {
            var topicName = "events";
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new ServiceBusMessage(new BinaryData(eBytes))
            {
                MessageId = Guid.NewGuid().ToString()
            };

            var client = new ServiceBusClient(EventServiceBusConnectionString);
            var sender = client.CreateSender(topicName);
            await sender.ScheduleMessageAsync(bm, delay);
        }

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new ServiceBusMessage(new BinaryData(eBytes))
            {
                MessageId = Guid.NewGuid().ToString()
            };

            var client = new ServiceBusClient(EventServiceBusConnectionString);
            var sender = client.CreateSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public void CreateTopicMessages(List<EventInfo> events)
        {

        }

        public void CreateTopicMessages(string topicName, List<EventInfo> events)
        {

        }

        public async Task CreateTopicMessagesAsync(List<EventInfo> events)
        {
            await CreateTopicMessagesAsync("events", events);
        }

        public async Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events)
        {
            var client = new ServiceBusClient(EventServiceBusConnectionString);
            var sender = client.CreateSender(topicName);

            var messages = new Queue<ServiceBusMessage>();

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new ServiceBusMessage(new BinaryData(eBytes))
                {
                    MessageId = Guid.NewGuid().ToString()
                };
                messages.Enqueue(bm);
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

        }

        public async Task CreateTopicMessageAsync<T>(string topicName, T e)
        {
            await CreateTopicMessageAsync<T>(topicName, e);
        }

        public async Task CreateFilteredTopicMessageAsync(string topicName, IFilteredServiceBusMessage e)
        {
            var eString = JsonConvert.SerializeObject(e);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new ServiceBusMessage(new BinaryData(eBytes));
            if (!string.IsNullOrEmpty(e.EventType))
            {
                bm.ApplicationProperties.Add("EventType", e.EventType);
            }
            bm.MessageId = Guid.NewGuid().ToString();

            var client = new ServiceBusClient(EventServiceBusConnectionString);
            var sender = client.CreateSender(topicName);
            await sender.SendMessageAsync(bm);
        }

        public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
        {

        }

        public async Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
        {
            var client = new ServiceBusClient(EventServiceBusConnectionString);
            var sender = client.CreateSender(topicName);

            var messages = new Queue<ServiceBusMessage>();

            foreach (var e in events)
            {
                var eString = JsonConvert.SerializeObject(e);
                var eBytes = Encoding.UTF8.GetBytes(eString);
                var bm = new ServiceBusMessage(new BinaryData(eBytes))
                {
                    MessageId = Guid.NewGuid().ToString()
                };
                messages.Enqueue(bm);
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

        public string GetTopicNameBySetting(string settingName)
        {
            return ConfigurationManager.AppSettings[settingName];
        }
    }
}
