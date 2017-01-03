using d360.core.enums;
using d360.core.queue;
using System.Collections.Generic;

namespace d360.extensions
{
    public interface IQueueSource
    {
        void CreateMessage(QueueType type, QueueObject item);

        void CreateMessages(QueueType type, List<QueueObject> items);
    }
}
