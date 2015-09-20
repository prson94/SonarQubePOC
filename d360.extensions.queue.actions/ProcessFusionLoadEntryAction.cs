using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.queue;
using Autofac;

namespace d360.extensions.queue.actions
{
    public class ProcessFusionLoadEntryAction : IQueueAction
    {
        public bool ProcessMessage(IContainer Container, QueueObject item)
        {
            var success = false;
            try
            {
                var model = (ProcessFusionLoadEntryModel)item;
            }
            catch (Exception ex)
            {
                success = false;
            }
            return success;
        }
    }
}
