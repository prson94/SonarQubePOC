using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessFusionCache
{
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();
                    var queueItems = companyConnection.Query<dynamic>(@"select top 1 ID, FusionID from [queue].FusionCache where MachineAssigned is null and NumberOfRetries < 5").ToList();

                    Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].FusionCache set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            bool processFusionWriteStatus = true;
                            var processFusionTask = companyConnection.ExecuteAsync("exec fusion.ProcessFusionCacheInQueue @FusionID", new { FusionID = q.FusionID }, null, 10800);    // 180 minute timeout.
                            processFusionTask.ContinueWith(t =>
                            {
                                string exceptionData = "";
                                if (t.Exception != null)
                                {
                                    exceptionData = t.Exception.GetFullExceptionData();
                                    if (t.Exception.InnerExceptions != null)
                                    {
                                        foreach (var ex in t.Exception.InnerExceptions)
                                        {
                                            exceptionData += ex.GetFullExceptionData();
                                        }
                                    }
                                    mex.Add(t.Exception);
                                }

                                if (t.IsCompleted)
                                {
                                    if (t.IsFaulted)
                                    {
                                        companyConnection.Execute(@"update [queue].FusionCache set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = exceptionData }, null, 500);
                                    }
                                    else
                                    {
                                        companyConnection.Execute("delete [queue].FusionCache where ID = @queueID", new { queueID = q.ID }, null, 500);
                                    }
                                }

                                processFusionWriteStatus = false;
                            });

                            while (processFusionWriteStatus)
                            {
                                Console.WriteLine("Process fusion cache procedure executing...");
                                System.Threading.Thread.Sleep(30000);
                            }
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                        }
                    });

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Trace.TraceError(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
