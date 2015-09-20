using d360.core.queue;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace d360.extensions.queue.azure
{
    public class QueueSource : IQueueSource
    {
        /*
         * RootManageSharedAccessKey
         *      Endpoint=sb://d3s-actions.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=PtVxYjJwvu1pgqHdBIHm43u23jAtoG6YL88YySkW6OA=
         * 
         */

        string queueConnectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.QueueConnectionString"];

        public void CreateMessage(QueueObject item)
        {
            //var namespaceManager = NamespaceManager.CreateFromConnectionString(queueConnectionString);
            //if (!namespaceManager.QueueExists(QueueName))
            //{
            //    namespaceManager.CreateQueue(QueueName);
            //}
            var queueClient = QueueClient.CreateFromConnectionString(queueConnectionString, "company-actions");
            var m = new BrokeredMessage(item);
            m.To = item.To.ToString();

            queueClient.Send(m);

            queueClient = null;
        }
    }
}
