using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.ProcessAzureQueues
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }
}
