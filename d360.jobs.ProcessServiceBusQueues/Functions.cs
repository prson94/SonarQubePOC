using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.ProcessServiceBusQueues
{
    public class Functions
    {
        public static void ProcessQueueMessage([ServiceBusTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }
    }
}
