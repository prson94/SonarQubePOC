using d360.core;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Linq;

namespace igx.jobs.scoreprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
