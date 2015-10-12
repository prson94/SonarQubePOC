using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using Dapper;
using System.IO;
using d360.core.entities;
using SpreadsheetLight;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using d360.core.entities.Queues;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace d360.jobs.queue.ProcessBulkLoad
{
    public class LoadContext : DbContext
    {
        public LoadContext(string connectionString): base(connectionString)
        {

        }

        public ObjectContext ObjectContext
        {
            get
            {
                try
                {
                    return ((IObjectContextAdapter)this).ObjectContext;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public DbSet<BulkLoadQueue> BulkLoadQueues { get; set; }

        public DbSet<Load> Loads { get; set; }
        public DbSet<LoadItem> LoadItems { get; set; }
        public DbSet<LoadItemColumn> LoadItemColumns { get; set; }
        public DbSet<LoadColumn> LoadColumns { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);

            base.Configuration.AutoDetectChangesEnabled = false;
            base.Configuration.ProxyCreationEnabled = false;
            base.Configuration.LazyLoadingEnabled = false;
        }
    }

    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 4).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var ctx = new LoadContext(GetCompanyConnectionString(companyID));

                    var queueItems = ctx.BulkLoadQueues.Where(i => i.MachineAssigned == null && i.NumberOfRetries < 3).OrderBy(i => i.LoadID).Take(2).ToList();

                    queueItems.ForEach(q =>
                    {
                        q.MachineAssigned = Environment.MachineName;
                    });
                    ctx.SaveChanges();

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            var load = ctx.Loads.Include(i => i.LoadColumns).SingleOrDefault(i => i.ID == q.LoadID);

                            Console.WriteLine("Company: {0}. Processing Load {1}", companyID, load.ID);

                            var memoryStream = new MemoryStream(load.File);
                            var xls = new SLDocument(memoryStream);

                            var stats = xls.GetWorksheetStatistics();

                            var numberOfRows = stats.NumberOfRows;
                            var rowIndex = stats.StartRowIndex + 1;
                            while (rowIndex <= stats.EndRowIndex)
                            {
                                var loadItem = new LoadItem { LoadID = load.ID, RowIndex = rowIndex, LoadItemColumns = new List<LoadItemColumn>() };

                                foreach (var c in load.LoadColumns.OrderBy(i => i.ColumnIndex))
                                {
                                    loadItem.LoadItemColumns.Add(new LoadItemColumn { ColumnIndex = c.ColumnIndex, LoadID = load.ID, RowIndex = rowIndex, Value = xls.GetCellValueAsString(rowIndex, c.ColumnIndex) });
                                }

                                ctx.LoadItems.Add(loadItem);

                                rowIndex++;
                            }

                            ctx.SaveChanges();  // Save all load items and columns we created.

                            Console.WriteLine("Company: {0}. Executing ProcessBulkLoad procedure for Load {1}", companyID, load.ID);

                            bool writeStatus = true;
                            var task = ctx.ObjectContext.Connection.ExecuteAsync("exec ProcessBulkLoad @LoadID", new { LoadID = load.ID }, null, 1800);
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

                            ctx.BulkLoadQueues.Remove(q);
                            ctx.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            q.NumberOfRetries++;
                            q.HasError = true;
                            q.ErrorMessage = ex.GetFullExceptionData();
                            ctx.SaveChanges();
                        }
                    });

                    ctx.Dispose();
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
