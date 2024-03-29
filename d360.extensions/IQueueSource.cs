using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using d360.core.queue;

namespace d360.extensions
{
    public interface IQueueSource
    {
        string GetMessageIdFromEventInfo(EventInfo eventInfo);
        bool CreateMessage<T>(string queueName, T item);
        bool CreateMessages<T>(string queueName, List<T> items);
        Task<bool> CreateMessageAsync<T>(string queueName, T item, TimeSpan? initialVisibilityDelay = null);
        Task<bool> CreateMessagesAsync<T>(string queueName, List<T> items, TimeSpan? initialVisibilityDelay = null);
    }
}
