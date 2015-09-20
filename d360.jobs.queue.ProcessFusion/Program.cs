using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessFusion
{
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs().Where(i => i == 15).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    var queueItems = companyConnection.Query<dynamic>(@"select top 2 * from [queue].Fusion where MachineAssigned is null and NumberOfRetries < 3").ToList();
                    
                    Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].Fusion set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            bool processFusionWriteStatus = true;
                            var processFusionTask = companyConnection.ExecuteAsync("exec fusion.ProcessFusionInQueue @queueID", new { queueID = q.ID }, null, 7200);    // 120 minute timeout.
                            processFusionTask.ContinueWith(t =>
                            {
                                if (t.IsCompleted)
                                    Console.WriteLine("Process fusion procedure completed for queue ID {0}, company {1}", q.ID, companyID);
                                if (t.IsFaulted)
                                    Console.WriteLine("Process fusion procedure failed for queue ID {0}, company {1}", q.ID, companyID);

                                if (t.Exception != null)
                                {
                                    if (t.Exception.InnerExceptions != null)
                                    {
                                        mex.AddRange(t.Exception.InnerExceptions);
                                    }
                                }
                                processFusionWriteStatus = false;
                            });

                            while (processFusionWriteStatus)
                            {
                                Console.WriteLine("Process fusion procedure executing...");
                                System.Threading.Thread.Sleep(15000);
                            }

                            companyConnection.Execute("delete [queue].Fusion where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].Fusion set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
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
