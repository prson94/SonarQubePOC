using d360.core;
using d360.core.entities;
using d360.fusion;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workers.FusionWorkerRole
{
    public class FusionQueueManager : IFusionQueueManager
    {
        private CloudQueueClient _queueClient;
        

        public FusionQueueManager()
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(acctName, keyValue), true);            
            _queueClient = storageAccount.CreateCloudQueueClient();                        
        }

        // Puts a serialized fixit onto the queue.
        public async Task SendMessageAsync(FusionProcessingData fusion)
        {
            CloudQueue queue = _queueClient.GetQueueReference(constants.AZURE_FUSION_QUEUE);
            await queue.CreateIfNotExistsAsync();

            var fusionJson = JsonConvert.SerializeObject(fusion);
            CloudQueueMessage message = new CloudQueueMessage(fusionJson);

            await queue.AddMessageAsync(message);
        }

        // Processes any messages on the queue.
        public async Task ProcessMessagesAsync()
        {
            CloudQueue queue = _queueClient.GetQueueReference(constants.AZURE_FUSION_QUEUE);
            await queue.CreateIfNotExistsAsync();
            

            while (true)
            {
                CloudQueueMessage message = await queue.GetMessageAsync();
                
                if (message == null)
                {
                    break;
                }
                FusionProcessingData fusion = JsonConvert.DeserializeObject<FusionProcessingData>(message.AsString);
                
                Trace.TraceInformation("FusionQueueManager loaded a message from the queue");
                Trace.TraceInformation("Message info, dequeue count [{0}], insert time [{1}]", message.DequeueCount, message.InsertionTime);
                // handle the fusion here
                FusionProcessor fp = new FusionProcessor();

                try
                {
                    await fp.Process(fusion);

                    Trace.TraceInformation("Fusion Processing successful! FusionQueueManager deleting message from queue");
                    await queue.DeleteMessageAsync(message);
                }
                catch (AggregateException exception)
                {
                    Trace.TraceError("FusionQueueManager encountered and error while running fusion job.");
                    foreach (Exception ex in exception.InnerExceptions)
                        Trace.TraceError("Exception details [{0}]", ex.Message);                    
                }
                catch (Exception ex)
                {
                    Trace.TraceError("FusionQueueManager encountered and error while running fusion job.  Exception details [{0}]",ex.Message);                    
                }
            }
        }
    }
}
