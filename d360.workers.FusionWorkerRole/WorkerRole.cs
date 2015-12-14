using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using d360.core;
using d360.core.entities;
using System.IO;

namespace d360.workers.FusionWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        public override void Run()
        {
            Trace.TraceInformation("d360.workers.FusionWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("d360.workers.FusionWorkerRole has been started");

#if DEBUG
            FusionProcessingData fusionData = new FusionProcessingData();
            IFusionQueueManager queueManager = new FusionQueueManager();
            using (StreamReader r = new StreamReader("c:\\test.json"))
            {
                string json = r.ReadToEnd();
                BulkFusionImport items = JsonConvert.DeserializeObject<BulkFusionImport>(json);

                fusionData.CompanyID = 4;
                fusionData.FusionID = 45;
                fusionData.LogFileName = "1.45.2015-12-10_07.28.12.json";
            }

            //save test data to queue
            queueManager.SendMessageAsync(fusionData).Wait();
#endif

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("d360.workers.FusionWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("d360.workers.FusionWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            IFusionQueueManager queueManager = new FusionQueueManager();
            
            while (!cancellationToken.IsCancellationRequested)
            {                
                await queueManager.ProcessMessagesAsync();
            }
        }
        
    }
}
