using d360.core.queue;
using Microsoft.ServiceBus.Messaging;
using d360.core.enums;
using d360.core;
using System;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Diagnostics;

namespace d360.extensions.queue
{
    public class AzureQueueSource : IQueueSource
    {
        private StorageCredentials getCredentials()
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            return new StorageCredentials(acctName, keyValue);
        }

        public void CreateMessage(QueueType type, QueueObject item)
        {
            //var connectionString = constants.SERVICE_BUS_ACTIONS;
            var queueName = "";

            switch (type)
            { 
                case QueueType.CommunityAction:
                    queueName = "community-actions";
                    break;
                case QueueType.CommunityProcess:
                    queueName = "community-processes";
                    break;
                case QueueType.CompanyAction:
                    queueName = "d3s-actions";//"company-actions";
                    break;
                case QueueType.CompanyProcess:
                    queueName = "company-processes";
                    break;
            }

            try
            {
                var queueClient = new Microsoft.WindowsAzure.Storage.Queue.CloudQueueClient(
                                    new Uri(string.Format(@"https://{0}.queue.core.windows.net/", constants.AZURE_STORAGE_NAME)),
                                    getCredentials());

                var msg = new CloudQueueMessage(JsonConvert.SerializeObject(item));

                var queue = queueClient.GetQueueReference(queueName);
                queue.AddMessage(msg);

                queue = null;
                queueClient = null;

                //var queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
                //var m = new BrokeredMessage(item);
                //m.To = item.To.ToString();
                //queueClient.Send(m);
                //queueClient = null;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occured trying to connect to Azure queue.  Error is: {0} {1}", ex.Message, (ex.InnerException != null ? ex.InnerException.Message : ""));
            }
        }


    }
}
