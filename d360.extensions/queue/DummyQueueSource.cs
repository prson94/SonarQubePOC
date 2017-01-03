using d360.core.enums;
using d360.core.queue;
using System.Collections.Generic;

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

    }
}
