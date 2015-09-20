using d360.core.enums;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace d360.extensions
{
    public interface IQueueSource
    {
        void CreateMessage(QueueType type, QueueObject item);
    }
}
