using d360.core;
using d360.core.entities;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace d360.fusion
{
    public class FusionQueueManager : IFusionQueueManager
    {
        private CloudQueueClient _queueClient;
        private string _queueName;

        public FusionQueueManager(string queueName)
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            _queueName = queueName;
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(acctName, keyValue), true);            
            _queueClient = storageAccount.CreateCloudQueueClient();                        
        }

        // Puts a serialized fixit onto the queue.
        public async Task SendMessageAsync(FusionProcessingData fusion)
        {
            CloudQueue queue = _queueClient.GetQueueReference(_queueName);
            await queue.CreateIfNotExistsAsync();

            var fusionJson = JsonConvert.SerializeObject(fusion);
            CloudQueueMessage message = new CloudQueueMessage(fusionJson);

            await queue.AddMessageAsync(message);
        }

        // Processes any messages on the queue.
        public async Task ProcessMessagesAsync(int messageReservationTime = 1800,
                                                int bulkTimeout = 180,
                                                int readTimeout = 180,
                                                int executionTimeout = 180,
                                                int maxRetries = 3,
                                                int queueCheckFrequency = 5000)
        {
            CloudQueue queue = _queueClient.GetQueueReference(_queueName);
            await queue.CreateIfNotExistsAsync();
            

            while (true)
            {
                TimeSpan resevationTime = new TimeSpan(0,0,messageReservationTime);
                CloudQueueMessage message = await queue.GetMessageAsync(resevationTime,null, null);
                
                if (message == null)
                {                    
                    await Task.Delay(queueCheckFrequency);

                    break;
                }


                if (message.DequeueCount > maxRetries)
                {
                    await queue.DeleteMessageAsync(message);

                    Trace.TraceError("MESSAGE HAS EXCEEDED THE MAX NUMBER OF RETRIES AND IS BEING DELETED.  CONTENT: {0}", message.AsString);
                }

                FusionProcessingData fusion = JsonConvert.DeserializeObject<FusionProcessingData>(message.AsString);
                
                Trace.TraceInformation("FusionQueueManager loaded a message from the queue");
                Trace.TraceInformation("Message info, dequeue count [{0}], insert time [{1}]", message.DequeueCount, message.InsertionTime);
                // handle the fusion here
                FusionProcessor fp = new FusionProcessor();

                try
                {
                    Trace.TraceInformation("Starting new task to process this fusion.");
                    
                    var t = Task.Run(async delegate
                    {
                        try
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            await fp.Process(fusion, bulkTimeout,readTimeout, executionTimeout);
                            Trace.TraceInformation(string.Format("Fusion Processing Took\tTIME ELAPSED {0} MS", sw.ElapsedMilliseconds));

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
                            Trace.TraceError("FusionQueueManager encountered and error while running fusion job.  Exception details [{0}]", ex.Message);
                        }
                    });

                }                
                catch (Exception ex)
                {
                    Trace.TraceError("FusionQueueManager encountered and error while running fusion job.  Exception details [{0}]",ex.Message);                    
                }
            }
        }
    }
}
