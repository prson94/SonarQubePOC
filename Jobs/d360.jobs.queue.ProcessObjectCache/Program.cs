using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessObjectCache
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

                    #region HACK FOR INTERSECTS
                    try
                    {
                        companyConnection.Execute(@"insert into [queue].[ObjectCache] ([Object], ObjectID)
	select [Object], ObjectID from cache.ObjectDetails where Name = 'Name cannot be resolved'");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.GetFullExceptionData());
                    }
                    #endregion

                    var queueItems = companyConnection.Query<dynamic>(@"select * from [queue].ObjectCache where MachineAssigned is null and NumberOfRetries < 3").ToList();
                    
                    Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].ObjectCache set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            companyConnection.Execute("exec cache.SynchronizeObjectDetails @t, @id", new { t = q.Object, id = q.ObjectID }, null, 180);    // 3 minute timeout.
                            companyConnection.Execute("delete [queue].ObjectCache where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].ObjectCache set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
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
