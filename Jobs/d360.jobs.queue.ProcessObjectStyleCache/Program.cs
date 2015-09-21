using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessObjectStyleCache
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

                    var queueItems = companyConnection.Query<dynamic>(@"select * from [queue].ObjectStyleCache where MachineAssigned is null and NumberOfRetries < 3 order by Date asc").ToList();
                    Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].ObjectStyleCache set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try 
                        {
                            bool writeStatus = true;

                            var task = companyConnection.ExecuteAsync(
@"
update	T
set		T.IconBackColor = S.IconBackColor,
T.IconForeColor = S.IconForeColor,
T.IconText = S.IconText
from	cache.ObjectDetails T
inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.ObjectType = S.ObjectType and T.ObjectTypeID = S.ObjectID;

update	T
set		T.IconBackColor = S.IconBackColor,
T.IconForeColor = S.IconForeColor,
T.IconText = S.IconText
from	cache.ObjectDetails T
inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.[Object] = S.ObjectType and T.ObjectID = S.ObjectID;",
new { type = q.Object, id = q.ObjectID }, null, 7200);

                            task.ContinueWith(t =>
                            {
                                if (t.IsCompleted)
                                    Console.WriteLine("Style Cache Refresh completed for Object {0} ID {1}", q.Object, q.ObjectID);
                                if (t.IsFaulted)
                                    Console.WriteLine("Style Cache Refresh failed for Object {0} ID {1}", q.Object, q.ObjectID);
                                if (t.Exception != null)
                                {
                                    if (t.Exception.InnerExceptions != null)
                                    {
                                        mex.AddRange(t.Exception.InnerExceptions);
                                    }
                                }
                                writeStatus = false;
                            });

                            while (writeStatus)
                            {
                                Console.WriteLine("Caching object styles...");
                                System.Threading.Thread.Sleep(15000);
                            }

                            companyConnection.Execute("delete [queue].ObjectStyleCache where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].ObjectStyleCache set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
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
