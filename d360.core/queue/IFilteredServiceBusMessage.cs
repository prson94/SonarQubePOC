using System;
using System.Collections.Generic;
using System.Text;

namespace d360.core.queue
{
    public interface IFilteredServiceBusMessage
    {
        string EventType { get; set; }
    }
}
