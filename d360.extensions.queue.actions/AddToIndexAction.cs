using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.queue;
using Autofac;

namespace d360.extensions.queue.actions
{
    public class AddToIndexAction : IQueueAction
    {
        public bool ProcessMessage(IContainer Container, QueueObject item)
        {
            var success = false;
            try
            {
                var model = (AddToIndexModel)item;
                var source = Container.Resolve<ISearchSource>();
            }
            catch (Exception ex)
            {
                success = false;
            }
            return success;
        }
    }
}
