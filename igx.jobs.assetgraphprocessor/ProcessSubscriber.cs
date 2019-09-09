using d360.core.queue;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.jobs.assetsyncprocessor2
{
    public class ProcessSubscriber
    {
        const string functionName = "AssetGraphProcessor_ProcessSubscriber";
        public static async Task Run([QueueTrigger("%DisplayValueQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var updateInfo = JsonConvert.DeserializeObject<GraphInfo>(myQueueItem);

        }
    }
}
