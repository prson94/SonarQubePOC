using d360.core.enums;
using d360.core.queue;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
    public class DummyQueueSource: IQueueSource
    {
        public void CreateMessage(QueueType type, QueueObject item)
        {
        }

        public void CreateMessages(QueueType type, List<QueueObject> items)
        {

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
            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, topicName); //Microsoft.ServiceBus.ConnectionString in app.config
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

            var client = TopicClient.CreateFromConnectionString(core.constants.EVENTS_SERVICE_BUS, "Events"); //Microsoft.ServiceBus.ConnectionString in app.config
            await client.SendBatchAsync(list);
        }

    }
}
