using d360.core;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions
{
    public interface IQueueSource
    {
        void CreateMessage<T>(string queueName, T item);

        void CreateMessages<T>(string queueName, List<T> items);

        Task CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null);

        Task CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null);

        void CreateTopicMessage(EventInfo e);
        void CreateTopicMessage(string topicName, EventInfo e);
        void CreateTopicMessage<T>(string topicName, T e);

        Task CreateTopicMessageAsync(EventInfo e);
        Task CreateTopicMessageAsync(string topicName, EventInfo e);
        Task CreateTopicMessageAsync<T>(string topicName, T e);

        void CreateTopicMessages(List<EventInfo> events);
        void CreateTopicMessages(string topicName, List<EventInfo> events);
        void CreateTopicMessages<T>(string topicName, List<T> events);

        Task CreateTopicMessagesAsync(List<EventInfo> events);
        Task CreateTopicMessagesAsync(string topicName, List<EventInfo> events);

        string GetTopicNameBySetting(string settingName);
        Task CreateTopicMessagesAsync<T>(string topicName, List<T> events);
    }
}
