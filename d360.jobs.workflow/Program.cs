using System;
using System.Linq;
using System.Activities;
using System.Activities.Statements;
using System.ServiceModel.Activities;
using System.Activities.DurableInstancing;
using d360.core;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Activities.Tracking;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.workflow
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));
            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }
    }


}
