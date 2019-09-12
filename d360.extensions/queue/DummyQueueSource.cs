using d360.core.enums;
using d360.core.queue;
using Microsoft.Azure;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
    public class DummyQueueSource: IQueueSource
    {
        public string EventServiceBusConnectionString { get { return CloudConfigurationManager.GetSetting("EventServiceBus"); } }

        public void CreateMessage<T>(string queueName, T item)
        {
        }

        public void CreateMessages<T>(string queueName, List<T> items)
        {

        }

        public async Task CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null)
        {
            await Task.Run(() => { });
        }

        public async Task CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null)
        {
            await Task.Run(() => { });
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
            var bm = new BrokeredMessage(e);
            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, topicName); 
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
            var list = new List<BrokeredMessage>();
            foreach (var e in events)
            {
                var bm = new BrokeredMessage(e);
                list.Add(bm);
            }

            var client = TopicClient.CreateFromConnectionString(EventServiceBusConnectionString, "Events");
            await client.SendBatchAsync(list);
        }

        public string GetTopicNameBySetting(string settingName)
        {
            return CloudConfigurationManager.GetSetting(settingName);
        }

    }
}
