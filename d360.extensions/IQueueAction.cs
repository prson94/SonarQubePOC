using Autofac;
using d360.core.entities;
using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.extensions
{
    public interface IQueueAction
    {
        //bool ProcessMessage(QueueItem item);
        bool ProcessMessage(IContainer Container, QueueObject item);
    }
}
