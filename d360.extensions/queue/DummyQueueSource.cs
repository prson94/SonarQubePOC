using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.queue
{
    public class DummyQueueSource: IQueueSource
    {
        public void CreateMessage(core.enums.QueueType type, core.queue.QueueObject item)
        {
        }
    }
}
