using d360.core.enums;
using d360.core.queue;
using Microsoft.Azure;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
    public class DummyQueueSource: IQueueSource
    {
        public string EventServiceBusConnectionString { get { return CloudConfigurationManager.GetSetting("EventServiceBus"); } }

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

        private Message GetMessageFromObject(object o)
        {
            var eString = JsonConvert.SerializeObject(o);
            var eBytes = Encoding.UTF8.GetBytes(eString);
            var bm = new Message(eBytes);
            bm.MessageId = Guid.NewGuid().ToString();

            return bm;
        }

        private TopicClient CreateTopicClient(string topicName)
        {
            var client = new TopicClient(EventServiceBusConnectionString, topicName, DefaultTopicRetryPolicy);
            return client;
        }

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
            await Task.Run(() => {  });
            return true;
        }

        public async Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            await Task.Run(() => { });
            return true;
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

        public async Task CreateTopicMessageAsync(string topicName, EventInfo e)
        {
            var bm = GetMessageFromObject(e);
            var client = CreateTopicClient(topicName);
            await client.SendAsync(bm);
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
            var list = new List<Message>();
            var partitionKey = Guid.NewGuid().ToString();
            foreach (var e in events)
            {
                var bm = GetMessageFromObject(e);
                bm.PartitionKey = partitionKey;
                list.Add(bm);
            }

            var client = CreateTopicClient("Events");
            await client.SendAsync(list);
        }

        public void CreateTopicMessage<T>(string topicName, T e)
        {

        }

        public async Task CreateTopicMessageAsync<T>(string topicName, T e)
        {
            var bm = GetMessageFromObject(e);
            var client = CreateTopicClient(topicName);
            await client.SendAsync(bm);
        }

        public void CreateTopicMessages<T>(string topicName, List<T> events, DateTime? scheduledEnqueueTime = null)
        {

        }

        public async Task CreateTopicMessagesAsync<T>(string topicName, List<T> events)
        {
            var list = new List<Message>();
            var partitionKey = Guid.NewGuid().ToString();
            foreach (var e in events)
            {
                var bm = GetMessageFromObject(e);
                bm.PartitionKey = partitionKey;
                list.Add(bm);
            }

            var client = CreateTopicClient("Events");
            await client.SendAsync(list);
        }
        public string GetTopicNameBySetting(string settingName)
        {
            return CloudConfigurationManager.GetSetting(settingName);
        }

    }
}
