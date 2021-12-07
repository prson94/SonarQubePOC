using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.extensions.search;
using d360.extensions.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using d360.extensions.mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace igx.functions.databasetaskprocessor
{
    public class DatabaseTaskProcessor
    {
        const string functionName = "DatabaseTask_ProcessScheduled";
        const string timerSettings = "*/1 * * * * *";
        const int DEFAULT_QUEUE_ITEMS = 1000;
        private CoreFunction CoreFunction;

        [FunctionName("DatabaseTaskProcessor")]
        public async Task Run([TimerTrigger(timerSettings, RunOnStartup = true)] TimerInfo myTimer, System.IO.TextWriter log, Microsoft.Azure.WebJobs.ExecutionContext context)
        {
            try
            {
                var config = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();

                CoreFunction = new CoreFunction(config);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 4).ToList();
#endif

                companies.Shuffle(); //Randomize
                companies.ForEach(c =>
                {
                    try
                    {
                        var numberOfQueueItems = DEFAULT_QUEUE_ITEMS;
                        if (int.TryParse(CoreFunction.GetConfigValueByKey<string>("TaskProcessorNumQueueItems"), out int tempNumQueueItems))
                        {
                            numberOfQueueItems = tempNumQueueItems > 0 ? tempNumQueueItems : DEFAULT_QUEUE_ITEMS;
                        }

                        var indexCollectionModel = new ObjectIndexCollectionModel();

                        using (var outerCompanyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(c.CompanyID, c.Server, c.Username, c.Password)))
                        {
                            outerCompanyConnection.Open();

                            #region Indexer Func

                            Func<SqlConnection, string, int, string, long, string> resolveIndexItem = (companyConnection, o, oid, a, givenAssetId) =>
                            {
                                if (!SearchIndexer.IsIndexable(o)) return string.Empty;

                                if (a == "Path")
                                {
                                    indexCollectionModel.UpsertPathByAssetId.Add(givenAssetId);
                                }
                                else if (a == "D") //Delete - asset is no longer present, so we can only use given parameters
                                {
                                    IndexObjectModel indexObject = new IndexObjectModel
                                    {
                                        CompanyID = c.CompanyID,
                                        Category = SearchIndexer.GetCategoryFromObject(o),
                                        ID = oid,
                                        To = QueueAction.RemoveFromIndex,
                                        RelativeUrl = "#"
                                    };

                                    if (givenAssetId > 0)
                                    {
                                        indexObject.AssetID = givenAssetId;
                                        indexObject.ItemUniqueID = givenAssetId.ToString();
                                    }
                                    //Set uniqueID for index object
                                    if (o == "Synonym")
                                    {
                                        indexObject.ItemUniqueID = $"custom|{oid}";
                                    }
                                    //Intersects have two search documents, se we need to delete both
                                    else if (o == "Intersect")
                                    {
                                        indexObject.Category = "Synonym";
                                        indexObject.AssetType = "Synonym";

                                        IndexObjectModel reciprocal = indexObject.ShallowCopy();
                                        reciprocal.ItemUniqueID = $"intersect|{oid}|O";
                                        indexObject.ItemUniqueID = $"intersect|{oid}|S";
                                        indexCollectionModel.Deletes.Add(reciprocal);
                                    }

                                    indexCollectionModel.Deletes.Add(indexObject);
                                }
                                else //Add or update
                                {
                                    if (o == "Synonym" || o == "ReferenceItemType")
                                    {
                                        //These objects are not assets, so they do not have an Asset UID
                                        indexCollectionModel.UpsertByObject.Add(new Tuple<string, long>(o, oid));
                                    }
                                    else if (o == "Intersect" && givenAssetId > 0)
                                    {
                                        //Intersects of Predicate type 6 are synonyms and are indexed
                                        bool isSynonym = companyConnection.Query<bool>(@"SELECT COUNT(1)
                                            FROM [dbo].[Intersect] i
                                            WHERE EXISTS (SELECT 1 FROM [dbo].[IntersectType] it
                                                INNER JOIN [dbo].[Predicate] p ON it.PredicateID = p.id
                                                WHERE p.type = 6 AND i.IntersectTypeID = it.ID)
                                            AND i.id = @a", new { a = oid }).SingleOrDefault();

                                        if (isSynonym)
                                        {
                                            indexCollectionModel.UpsertByObject.Add(new Tuple<string, long>(o, oid));
                                        }
                                    }
                                    else
                                    {
                                        Guid AssetUid = (givenAssetId > 0) ?
                                            companyConnection.Query<Guid>("SELECT Uid FROM [dbo].[Asset] WHERE id = @a", new { a = givenAssetId }).SingleOrDefault() :
                                            companyConnection.Query<Guid>("SELECT Uid FROM [dbo].[Asset] WHERE [Object] = @t AND [ObjectID] = @i", new { t = o, i = oid }).SingleOrDefault();

                                        if (AssetUid != Guid.Empty)
                                        {
                                            indexCollectionModel.UpsertByUid.Add(AssetUid);
                                        }
                                    }
                                }

                                return string.Empty;
                            };

                            #endregion

                            //dont bother doing anything if the queue.task table is empty
                            var existsSql = @"IF EXISTS (SELECT * FROM [queue].task where MachineAssigned is null and NumberOfRetries < 2)
                                                BEGIN
                                                    select 1;
                                                END
                                                ELSE
                                                BEGIN
                                                   select 0;
                                                END";

                            bool hasWork = false;
                            try
                            {
                                hasWork = outerCompanyConnection.QuerySingle<bool>(existsSql);
                            }
                            catch (SqlException ex)
                            {
                                //When doing a clean DB install, the queue.task table will not exist
                                //for some time. If the table is not present, there is no work to be done
                                //by this processor, so the error is muted.
                                if (ex.Message != "Invalid object name 'queue.task'.")
                                {
                                    throw;
                                }
                            }

                            if (hasWork)
                            {

                                var checkoutAndGetQueueItemSql = $@"
                                    declare @IDs table (ID uniqueidentifier)

                                    ;WITH CTE AS 
                                    ( 
                                        SELECT TOP {numberOfQueueItems} * 
                                        FROM [queue].[task]
                                        where MachineAssigned is null and NumberOfRetries < 2  and [date] < DATEADD(second, -30, getutcdate()) 
                                        ORDER BY [Date] ASC
                                    ) 
                                    UPDATE CTE set MachineAssigned = @m OUTPUT deleted.ID into @IDs  

                                    select  T.* 
                                    from    [queue].[Task] T
                                            inner join @IDs S on S.ID = T.ID
                                    ";

                                List<QueueTask> queueItems = null;

                                // Checkout select and update should be done in transaction to avoid other webjob instances from
                                // checking out the same items.  
                                using (var trans = outerCompanyConnection.BeginTransaction())
                                {
                                    try
                                    {
                                        queueItems = outerCompanyConnection.Query<QueueTask>(checkoutAndGetQueueItemSql, new { m = new DbString { Value = System.Environment.MachineName, IsAnsi = true, Length = 250 } }, transaction: trans).ToList();

                                        trans.Commit();
                                    }
                                    catch (Exception ex)
                                    {
                                        try
                                        {
                                            if (trans != null)
                                            {
                                                trans.Rollback();
                                            }
                                        }
                                        catch
                                        {
                                        }

                                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                                    }
                                }

                                if (queueItems != null)
                                {
                                    queueItems.AsParallel().ForAll(async q =>
                                    {
                                        try
                                        {
                                            using (var companyConnection = new SqlConnection(CompanyConnectionUtils.GetConnectionString(c.CompanyID, c.Server, c.Username, c.Password)))
                                            {
                                                companyConnection.Open();

                                                try
                                                {
                                                    switch (q.Action)
                                                    {
                                                        case "Add":
                                                            #region
                                                            addAuditEntry(companyConnection, "Created", q);
                                                            resolveIndexItem(companyConnection, q.Object, q.ObjectID, "A", q.AssetID);
                                                            break;
                                                        #endregion
                                                        case "Delete":
                                                            #region                                     
                                                            addAuditEntry(companyConnection, "Removed", q);
                                                            resolveIndexItem(companyConnection, q.Object, q.ObjectID, "D", q.AssetID);
                                                            break;
                                                        #endregion
                                                        case "EventTopicNotification":
                                                            #region
                                                            if (!string.IsNullOrEmpty(q.Custom))
                                                            {
                                                                var customXml = XElement.Parse(q.Custom);

                                                                var queue = new AzureQueueSource();

                                                                d360.core.enums.Workflow.ChangeType ct;
                                                                if (Enum.TryParse<d360.core.enums.Workflow.ChangeType>(customXml.Element("ChangeType").Value, out ct))
                                                                {
                                                                    SystemObjects obj;
                                                                    SystemObjects objType;
                                                                    if (Enum.TryParse<SystemObjects>(customXml.Element("ObjectType").Value, out objType))
                                                                    {
                                                                        if (Enum.TryParse<SystemObjects>(q.Object, out obj))
                                                                        {
                                                                            if (int.TryParse(customXml.Element("ObjectTypeID").Value, out int objectTypeID))
                                                                            {
                                                                                var topicName = c.EventTopic;
#if DEBUG
                                                                                topicName = "events-debug";
#endif
                                                                                queue.CreateTopicMessage(topicName, new EventInfo
                                                                                {
                                                                                    Action = ct,
                                                                                    CompanyID = c.CompanyID,
                                                                                    DomainPrefix = c.UrlPrefix,
                                                                                    Object = new EventObjectInfo
                                                                                    {
                                                                                        Object = obj,
                                                                                        ObjectID = q.ObjectID,
                                                                                        ObjectType = objType,
                                                                                        ObjectTypeID = objectTypeID
                                                                                    },
                                                                                    ResourceID = 0
                                                                                });
                                                                            }
                                                                            else { throw new ApplicationException("Unable to parse the ObjectTypeID specified."); }
                                                                        }
                                                                        else
                                                                        {
                                                                            throw new ApplicationException("Unable to identify the Object specified.");
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        throw new ApplicationException("Unable to identify the ObjectType specified.");
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    throw new ApplicationException("Unable to identify the ChangeType specified.");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                throw new ApplicationException("XML field does not have any valid information contained within.");
                                                            }
                                                            #endregion
                                                            break;
                                                        case "ObjectIndex":
                                                            #region
                                                            resolveIndexItem(companyConnection, q.Object, q.ObjectID, q.Custom, q.AssetID);
                                                            break;
                                                        #endregion
                                                        case "Update":
                                                            #region
                                                            addAuditEntry(companyConnection, "Updated", q);

                                                            if (q.Object != "PolicyType" && q.Object != "TaxonomyType")
                                                                resolveIndexItem(companyConnection, q.Object, q.ObjectID, "U", q.AssetID);
                                                            break;
                                                        #endregion
                                                        case "TagConsolidated":
                                                            addAuditEntry(companyConnection, "Tag Consolidate", q);
                                                            break;
                                                        case "CompanySettingsUpdate":
                                                            addAuditEntry(companyConnection, "Update settings", q);
                                                            break;
                                                        case "QueueRebuild":
                                                            if (!string.IsNullOrEmpty(q.Custom))
                                                            {
                                                                var queue = new AzureQueueSource();
                                                                switch (q.Custom)
                                                                {
                                                                    case "AssetGraph":
                                                                        queue.CreateMessage(Config.GetValue<string>("AssetGraphQueue"), new RebuildAssetGraphModel { CompanyID = c.CompanyID });
                                                                        break;
                                                                    case "DisplayValue":
                                                                        queue.CreateMessage(Config.GetValue<string>("DisplayValueQueue"), new DisplayUpdateInfo { CompanyID = c.CompanyID, RebuildAll = true });
                                                                        break;
                                                                    case "SearchIndex":
                                                                        queue.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel { CompanyID = c.CompanyID });
                                                                        break;
                                                                }
                                                            }
                                                            break;
                                                    }


                                                    companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                                }
                                                catch (Exception ex)
                                                {
                                                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                                                    try
                                                    {
                                                        if (q.NumberOfRetries >= 2)
                                                            companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                                        else
                                                            companyConnection.Execute(@"update [queue].[Task] set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
                                                    }
                                                    catch (Exception iex)
                                                    {
                                                        CoreFunction.AITrackException(functionName, iex, c.CompanyID);
                                                    }
                                                }

                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            CoreFunction.AITrackException(functionName, ex);
                                        }
                                    });
                                }

                            }

                        }

                        #region Now deal with INDEXING

                        try
                        {
                            var search = new ElasticSearchSource(config.GetConnectionStringOrSetting("CommunityContext"));

                            if (indexCollectionModel.Adds.Count > 0)
                            {
                                search.AddToIndex(indexCollectionModel.Adds);
                            }

                            if (indexCollectionModel.Deletes.Count > 0)
                            {
                                search.RemoveFromIndex(indexCollectionModel.Deletes);
                            }

                            if (indexCollectionModel.Updates.Count > 0)
                            {
                                search.UpdateInIndex(indexCollectionModel.Updates);
                            }
                            if (indexCollectionModel.ContainsIndexerCollections())
                            {
                                try
                                {
                                    using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, config.GetConnectionStringOrSetting("CommunityContext")))
                                    {
                                        companyConnection.Open();
                                        SearchIndexer indexer = new SearchIndexer(companyConnection, c.CompanyID, search);

                                        if (indexCollectionModel.UpsertByUid.Any())
                                        {
                                            indexer.IndexAssets(indexCollectionModel.UpsertByUid);
                                        }

                                        if (indexCollectionModel.UpsertByObject.Any())
                                        {
                                            indexer.IndexAssets(indexCollectionModel.UpsertByObject);
                                        }

                                        if(indexCollectionModel.UpsertPathByAssetId.Any())
                                        {
                                            indexer.IndexUpdateAssetPaths(indexCollectionModel.UpsertPathByAssetId);
                                        }

                                        indexer = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    CoreFunction.AITrackException(functionName, ex);
                                }
                            }

                            search = null;
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex);
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }


        private static void addAuditEntry(SqlConnection companyConnection, string oper, QueueTask queueRecord)
        {
            if (!string.IsNullOrEmpty(queueRecord.Custom))
            {
                var customXml = XElement.Parse(queueRecord.Custom);

                var parameters = new DynamicParameters();

                parameters.Add("@MainObject", queueRecord.Object, DbType.AnsiString, size: 50);
                parameters.Add("@MainObjectID", queueRecord.ObjectID);
                parameters.Add("@DependentObject", customXml.Element("ActionObject").Value, System.Data.DbType.AnsiString, size: 50);
                parameters.Add("@DependentObjectID", int.Parse(customXml.Element("ActionObjectID").Value));
                parameters.Add("@Date", queueRecord.Date);
                parameters.Add("@ResourceID", int.Parse(customXml.Element("ResourceID").Value));
                parameters.Add("@Action", oper, DbType.AnsiString, size: 15);
                parameters.Add("@NewValue", (customXml.Element("ActionObjectValue") == null ? null : customXml.Element("ActionObjectValue").Value), System.Data.DbType.AnsiString, size: 50);

                if (customXml.Element("FieldInfo") != null)
                {
                    parameters.Add("@AuditFieldTable", getFieldsTable(customXml.Element("FieldInfo")).AsTableValuedParameter("[dbo].[AuditFieldTable]"));
                }

                companyConnection.Query(
                    "[utility].[AddAuditEntry]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure,
                    commandTimeout: 600
                    );
            }
        }

        private static DataTable getFieldsTable(XElement xElement)
        {
            var tb = new DataTable();

            tb.Columns.Add("FieldTypeID", typeof(int));
            tb.Columns.Add("FieldName", typeof(string));
            tb.Columns.Add("Value", typeof(string));

            foreach (var child in xElement.Elements())
            {
                var fieldRow = tb.NewRow();
                fieldRow["FieldName"] = (string)child.Element("Name") ?? "";
                fieldRow["FieldTypeID"] = int.Parse(child.Element("FieldTypeID") == null ? "" : child.Element("FieldTypeID").Value);
                fieldRow["Value"] = (string)child.Element("Value") ?? "";

                tb.Rows.Add(fieldRow);
            }
            return tb;
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(System.Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class ObjectIndexCollectionModel
    {
        public ObjectIndexCollectionModel()
        {
            Adds = new ConcurrentBag<IndexObjectModel>();
            Deletes = new ConcurrentBag<IndexObjectModel>();
            Updates = new ConcurrentBag<IndexObjectModel>();
            UpsertByUid = new ConcurrentBag<Guid>();
            UpsertByObject = new ConcurrentBag<Tuple<string, long>>();
            UpsertPathByAssetId = new ConcurrentBag<long>();
        }

        public ConcurrentBag<IndexObjectModel> Adds { get; set; }
        public ConcurrentBag<IndexObjectModel> Deletes { get; set; }
        public ConcurrentBag<IndexObjectModel> Updates { get; set; }
        public ConcurrentBag<Guid> UpsertByUid { get; set; }
        public ConcurrentBag<Tuple<string, long>> UpsertByObject { get; set; }
        public ConcurrentBag<long> UpsertPathByAssetId { get; set; }

        public bool ContainsIndexerCollections()
        {
            return UpsertByObject.Any() || UpsertByUid.Any() || UpsertPathByAssetId.Any();
        }
    }

    public class QueueTask
    {
        public Guid ID { get; set; }
        public string Action { get; set; }
        public string Custom { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public DateTime Date { get; set; }
        public string MachineAssigned { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public int NumberOfRetries { get; set; }
        public short Priority { get; set; }
        public long AssetID { get; set; }
    }
}
