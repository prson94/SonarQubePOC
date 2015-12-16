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
            
            bool result = base.OnStart();

            Trace.TraceInformation("d360.workers.FusionWorkerRole has been started");

#if DEBUG
            IFusionQueueManager queueManager = new FusionQueueManager();

            FusionProcessingData fusionData = new FusionProcessingData
            {
                CompanyID = 4,
                FusionID = 46,
                   LogFileName = "1.45.2015-12-10_07.28.12.json"
             //   LogFileName = "1.45.modifytest.json" // file contains one row modified from base for fusion id 46.
            };

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
                await queueManager.ProcessMessagesAsync(GlobalStaticProperties.QueueMessageVisibilityTime);
            }
        }        
    }
    
}
