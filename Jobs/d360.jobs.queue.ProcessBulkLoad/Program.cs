using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.core;
using Dapper;
using System.Diagnostics;
using System.IO;
using d360.core.entities;
using SpreadsheetLight;

namespace d360.jobs.queue.ProcessBulkLoad
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

                    var queueItems = companyConnection.Query<dynamic>(@"select top 2 * from [queue].BulkLoad where MachineAssigned is null and NumberOfRetries < 3 order by LoadID asc").ToList();

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].BulkLoad set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            var load = companyConnection.Query<Load>("select * from Load where ID = @id", new { id = q.LoadID }).SingleOrDefault();

                            Console.WriteLine("Company: {0}. Processing Load {1}", companyID, load.ID);

                            var fields = companyConnection.Query<LoadTypeField>(
                                "select * from LoadTypeField where LoadTypeID = @id order by SortOrder",
                                new { id = load.LoadTypeID }
                            ).ToList();

                            var memoryStream = new MemoryStream(load.File);
                            var xls = new SLDocument(memoryStream);

                            var stats = xls.GetWorksheetStatistics();

                            var numberOfRows = stats.NumberOfRows;
                            var rowIndex = stats.StartRowIndex + 1;
                            while (rowIndex <= stats.EndRowIndex)
                            {
                                if(string.IsNullOrEmpty(xls.GetCellValueAsString(rowIndex, stats.StartColumnIndex)))
                                {
                                    numberOfRows--;
                                }
                                rowIndex++;
                            }

                            Console.WriteLine("Company: {0}. Load {1} has {2} rows to process", companyID, load.ID, numberOfRows - 1);

                            rowIndex = stats.StartRowIndex + 1;
                            while (rowIndex <= stats.EndRowIndex)
                            {
                                try
                                {
                                    if(!string.IsNullOrEmpty(xls.GetCellValueAsString(rowIndex, stats.StartColumnIndex)))
                                    {
                                        var loadItemID = companyConnection.ExecuteScalar<int>("insert into LoadItem (LoadID, RowIndex) values (@l, @r); select SCOPE_IDENTITY()", new { l = load.ID, r = rowIndex });
                                        var columnIndex = stats.StartColumnIndex;

                                        while (columnIndex <= stats.EndColumnIndex)
                                        {
                                            var field = fields[columnIndex - 1];
                                            if (field != null)
                                            {
                                                companyConnection.Execute("insert into LoadItemField (LoadItemID, LoadTypeFieldID, Value) values (@l, @f, @v)", new { l = loadItemID, f = field.ID, v = xls.GetCellValueAsString(rowIndex, columnIndex) });
                                            }
                                            columnIndex++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Company: {0}. Error occurred for Load {1}, Row {2}.  Error is: {3}", companyID, load.ID, rowIndex, ex.GetFullExceptionData());
                                }

                                rowIndex++;
                            }

                            Console.WriteLine("Company: {0}. Executing ProcessBulkLoad procedure for Load {1}", companyID, load.ID);

                            bool writeStatus = true;
                            var task = companyConnection.ExecuteAsync("exec ProcessBulkLoad @LoadID", new { LoadID = q.LoadID }, null, 1800);    // 30 minute timeout.
                            task.ContinueWith(t =>
                            {
                                if (t.IsCompleted)
                                    Console.WriteLine("Bulk load procedure completed for Load ID {0}", q.LoadID);
                                if(t.IsFaulted)
                                    Console.WriteLine("Bulk load procedure failed for Load ID {0}", q.LoadID);
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
                                Console.WriteLine(".");
                                System.Threading.Thread.Sleep(45000);
                            }

                            Console.WriteLine("Company: {0}. Finished executing ProcessBulkLoad procedure for Load {1}", companyID, load.ID);

                            companyConnection.Execute("delete [queue].BulkLoad where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].BulkLoad set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
                        }
                    });

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Console.WriteLine(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
