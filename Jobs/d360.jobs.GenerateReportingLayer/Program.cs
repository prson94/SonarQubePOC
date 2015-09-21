using System;
using d360.core;
using Microsoft.Azure.WebJobs;

namespace d360.jobs.GenerateReportingLayer
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = Functions.Generate();
            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
