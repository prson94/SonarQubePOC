using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessFusion
{
    //public class AzureConfiguration : DbConfiguration
    //{
    //    public AzureConfiguration()
    //    {
    //        SetExecutionStrategy("System.Data.SqlClient", () => new SqlAzureExecutionStrategy(3, TimeSpan.FromSeconds(5)));
    //    }
    //}

    //[DbConfigurationType(typeof(AzureConfiguration))]
    //public class FusionContext : DbContext
    //{
    //    public FusionContext(string connectionString): base(connectionString)
    //    {

    //    }

    //    public ObjectContext ObjectContext
    //    {
    //        get
    //        {
    //            try
    //            {
    //                return ((IObjectContextAdapter)this).ObjectContext;
    //            }
    //            catch (Exception ex)
    //            {
    //                throw ex;
    //            }
    //        }
    //    }

    //    public DbSet<Field> Fields { get; set; }
    //    public DbSet<FieldType> FieldTypes { get; set; }
    //    public DbSet<Fusion> Fusions { get; set; }
    //    public DbSet<FusionAttribute> FusionAttributes { get; set; }
    //    public DbSet<FusionAttributeType> FusionAttributeTypes { get; set; }
    //    public DbSet<FusionType> FusionTypes { get; set; }

    //    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    //    {
    //        modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
    //        modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

    //        base.OnModelCreating(modelBuilder);

    //        base.Configuration.AutoDetectChangesEnabled = true;
    //        base.Configuration.ProxyCreationEnabled = false;
    //        base.Configuration.LazyLoadingEnabled = false;
    //    }
    //}

    //public class Processor
    //{
    //    int CompanyID { get; set; }
    //    int FusionID { get; set; }
    //    int FusionTypeID { get; set; }

    //    SqlConnection companyConnection { get; set; }
    //    FusionContext companyContext { get; set; }

    //    BulkFusionImport model;
    //    List<FusionAttributeType> attributeTypes;
    //    List<FusionAttribute> currentAttributes;
    //    List<Field> currentAttributeFields;

    //    public Processor(int companyID, int fusionID, SqlConnection cnn, FusionContext ctx)
    //    {
    //        CompanyID = companyID;
    //        FusionID = fusionID;
    //        companyConnection = cnn;
    //        companyContext = ctx;

    //        var fusion = ctx.Fusions.SingleOrDefault(i => i.ID == fusionID); //companyConnection.Query<Fusion>("select * from Fusion where ID = @id", new { id = FusionID }).SingleOrDefault();
    //        FusionTypeID = fusion.FusionTypeID;
    //        fusion = null;

    //        var storage = new AzureStorageProvider();
    //        var folder = string.Format("bulk-fusion-{0}", companyID);
    //        string json = storage.GetFileContentsAsString(folder, string.Format("{0}.{1}.2015-10-23_08.13.23.json", FusionTypeID, FusionID));
    //        model = JsonConvert.DeserializeObject<BulkFusionImport>(json);
    //        json = null;
    //        storage = null;

    //        attributeTypes = ctx.FusionAttributeTypes.Where(i => i.FusionTypeID == FusionTypeID).ToList(); //companyConnection.Query<FusionAttributeType>("select * from FusionAttributeType where FusionTypeID = @id", new { id = FusionTypeID }).ToList(); //
    //        currentAttributes = ctx.FusionAttributes.Where(i => i.FusionID == FusionID).ToList(); //companyConnection.Query<FusionAttribute>("select * from FusionAttribute where FusionID = @id", new { id = FusionID }).ToList();
    //        //currentAttributeFields = (
    //        //                         from f in ctx.Fields
    //        //                         join a in ctx.FusionAttributes on f.ObjectID equals a.ID
    //        //                         where f.ObjectType == "FusionAttribute"
    //        //                         where a.FusionID == FusionID
    //        //                         select f
    //        //                         ).ToList();
    //    }

    //    public void LoadByParent(int? parentAttributeTypeID)
    //    {
    //        //var transaction = companyConnection.BeginTransaction();
    //        //var bulkCopy = new SqlBulkCopy(companyConnection, SqlBulkCopyOptions.Default, transaction);
    //        //bulkCopy.DestinationTableName = "dbo.FusionAttribute";
    //        //try
    //        //{
    //        //    var dt = new DataTable();
    //        //    dt.Columns.Add(new DataColumn("ParentID", typeof(int)));
    //        //    dt.Columns.Add(new DataColumn("Name", typeof(string)));
    //        //    dt.Columns.Add(new DataColumn("FusionID", typeof(int)));
    //        //    dt.Columns.Add(new DataColumn("FusionAttributeTypeID", typeof(int)));
    //        //    dt.Columns.Add(new DataColumn("SourceID", typeof(string)));
    //        //    dt.Columns.Add(new DataColumn("Deleted", typeof(bool)));

    //        //    bulkCopy.WriteToServer(dt);
    //        //}
    //        //catch (Exception ex)
    //        //{
    //        //    transaction.Rollback();
    //        //}

    //        attributeTypes.Where(at => at.ParentID == parentAttributeTypeID).ToList().ForEach(at =>
    //        {
    //            var attributeChangeCount = 0;

    //            //var attributesToSave = new List<FusionAttribute>();
    //            foreach (var m in model.Models.Where(m => m["FusionAttributeTypeID"] == at.ID.ToString()))
    //            {
    //                var currentAttribute = currentAttributes.FirstOrDefault(i => i.FusionAttributeTypeID == at.ID && i.FusionID == FusionID && i.SourceID == m["SourceID"]);

    //                #region Find parent
    //                int? parentAttributeID = null;
    //                if (m.ContainsKey("ParentSourceID") && parentAttributeTypeID.HasValue)
    //                {
    //                    var currentParentAttribute = model.Models.Where(i => i["FusionAttributeTypeID"] == parentAttributeTypeID.Value.ToString() && i["SourceID"] == m["ParentSourceID"]).FirstOrDefault();
    //                    if (currentParentAttribute != null)
    //                    {
    //                        parentAttributeID = int.Parse(currentParentAttribute["FusionAttributeID"]);
    //                    }
    //                }
    //                #endregion

    //                if (currentAttribute == null)
    //                {
    //                    currentAttribute = new FusionAttribute { FusionAttributeTypeID = at.ID, FusionID = FusionID, Name = m["Name"], SourceID = m["SourceID"], ParentID = parentAttributeID };
    //                    companyContext.FusionAttributes.Add(currentAttribute);
    //                    attributeChangeCount++;
    //                }
    //                else
    //                {
    //                    if (currentAttribute.ParentID != parentAttributeID || currentAttribute.Name != m["Name"])
    //                    {
    //                        currentAttribute.Name = m["Name"];
    //                        currentAttribute.ParentID = parentAttributeID;
    //                        attributeChangeCount++;
    //                    }
    //                }

    //                m.Add("FusionAttributeID", currentAttribute.ID.ToString());

    //                if (attributeChangeCount >= 250)
    //                {
    //                    companyContext.SaveChanges();
    //                    attributeChangeCount = 0;
    //                }
    //            }

    //            if (attributeChangeCount > 0)
    //            {
    //                companyContext.SaveChanges();
    //                attributeChangeCount = 0;
    //            }

    //            LoadByParent(at.ID);
    //        });
    //    }
    //}

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
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();
                    //var ctx = new FusionContext(GetCompanyConnectionString(companyID));
                    var queueItems = companyConnection.Query<dynamic>(@"select top 2 ID from [queue].Fusion where MachineAssigned is null and NumberOfRetries < 5").ToList();

                    Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].Fusion set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            //var processor = new Processor(companyID, 37, companyConnection, ctx); 
                            //processor.LoadByParent(null);


                            bool processFusionWriteStatus = true;
                            var processFusionTask = companyConnection.ExecuteAsync("exec fusion.ProcessFusionInQueue @queueID", new { queueID = q.ID }, null, 10800);    // 180 minute timeout.
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
                                    mex.Add(t.Exception);//companyConnection.Execute("insert into [fusion].[Error] values()", new { m = Environment.MachineName, queueID = q.ID });
                                }

                                if (t.IsCompleted)
                                {
                                    if (t.IsFaulted)
                                    {
                                        companyConnection.Execute(@"update [queue].Fusion set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = exceptionData }, null, 500);
                                    }
                                    else
                                    {
                                        companyConnection.Execute("delete [queue].Fusion where ID = @queueID", new { queueID = q.ID }, null, 500);
                                    }
                                }

                                processFusionWriteStatus = false;
                            });

                            while (processFusionWriteStatus)
                            {
                                Console.WriteLine("Process fusion procedure executing...");
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
