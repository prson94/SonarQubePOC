using d360.core;
using d360.core.enums;
using d360.core.enums.Workflow;
using d360.core.queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions
{
    public interface IQueueSource
    {
        void CreateMessage(QueueType type, QueueObject item);

        void CreateMessages(QueueType type, List<QueueObject> items);

        void CreateTopicMessage(EventInfo e);

        Task CreateTopicMessageAsync(EventInfo e);

        void CreateTopicMessages(List<EventInfo> events);

        Task CreateTopicMessagesAsync(List<EventInfo> events);
    }
}
