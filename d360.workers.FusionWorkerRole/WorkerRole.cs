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
using d360.core.entities;
using System.IO;
using d360.fusion;

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
            IFusionQueueManager queueManager = new FusionQueueManager(GlobalStaticProperties.QueueName);

            FusionProcessingData fusionData = new FusionProcessingData
            {
                CompanyID = 4,
                FusionID = 46,
           //     LogFileName = "no_models.json"
                LogFileName = "SELF_REFERENCE.JSON"
           //      LogFileName = "1.45.2015-12-10_07.28.12.json"
                // LogFileName = "1.45.modifytest.json" // file contains one row modified from base for fusion id 46.
            };
            
            //the biggest fusion job i can find 30.9 MB for Demo dev - gmo has a 35.3MB file in fusion-15 22 has 38.6mb
          /*  FusionProcessingData fusionData = new FusionProcessingData
            {
                CompanyID = 4,
                FusionID = 40,
                LogFileName = "1.40.2015-12-07_10.47.01.json"
                //   LogFileName = "1.45.modifytest.json" // file contains one row modified from base for fusion id 46.
            };*/


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
            IFusionQueueManager queueManager = new FusionQueueManager(GlobalStaticProperties.QueueName);
            
            while (!cancellationToken.IsCancellationRequested)
            {                
                // check the queue
                await queueManager.ProcessMessagesAsync(GlobalStaticProperties.QueueMessageVisibilityTime,
                                                        GlobalStaticProperties.DBBulkCopyTimeout,
                                                        GlobalStaticProperties.DBReadQueryTimeout,
                                                        GlobalStaticProperties.DBExecuteQueryTimeout, 
                                                        GlobalStaticProperties.MaximumRetries);

                // wait some time so we arent constantly polling the queue
                await Task.Delay(GlobalStaticProperties.QueueCheckFrequency);
            }
        }        
    }
    
}
