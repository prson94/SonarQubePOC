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
using Mandrill.Model;
using Microsoft.Azure.WebJobs;
using System.Collections.Concurrent;

namespace igx.functions.databasetaskprocessor
{    
    public static class DatabaseTaskProcessor
    {
        public static void SendMailToUser(string toName, string toEmail, string subject, string templateID, System.Collections.Generic.Dictionary<string, string> templateTags, string fromName = "Data3Sixty Workflow")
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = fromName;
            message.Subject = subject;

            message.TrackOpens = false;
            message.TrackClicks = false;


            if (templateTags != null)
            {
                foreach (var k in templateTags.Keys)
                {
                    message.AddRcptMergeVars(toEmail, k, templateTags[k]);
                }
            }

            //Add the HTML and Text bodies            
            var api = new Mandrill.MandrillApi(CoreFunction.GetConfigValueByKey("MandrillApiKey"));
            var resp = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }

        const string functionName = "DatabaseTask_ProcessScheduled";
        const string timerSettings = "*/1 * * * * *";
        const int markitLineageSettingID = 62;


        [FunctionName("DatabaseTaskProcessor")]
        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, System.IO.TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

#if DEBUG
                companies = companies.Where(i => i.CompanyID == 3).ToList();
#endif

                companies.Shuffle(); //Randomize

                #region Must keep this segment here b/c this webjob can execute on multiple machines.  We do not want two or more machines trying to grab hold of the same queue item.
                var rand = new Random();
                int sleepSeconds = rand.Next(1, 10);
                Thread.Sleep(sleepSeconds * 500);
                #endregion

                companies.AsParallel().ForAll(c =>
                {
                    try
                    {
                        var numberOfQueueItems = 1000;
                        var indexCollectionModel = new ObjectIndexCollectionModel();
                        List<CompanySetting> settings = null;

                        using (var outerCompanyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                        {
                            outerCompanyConnection.Open();

                            #region Indexer Func

                            Func<SqlConnection, string, int, string, long, string> resolveIndexItem = (companyConnection, o, oid, a, givenAssetId) =>
                            {
                                if (!SearchIndexer.IsIndexable(o)) return string.Empty;

                                if (a == "D") //Delete - asset is no longer present, so we can only use given parameters
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
                                    if (o == "Synonym" || o == "ReferenceItemType" || (o == "Intersect" && givenAssetId > 0))
                                    {
                                        //These objects are not assets, so they do not have an Asset UID
                                        indexCollectionModel.UpsertByObject.Add(new Tuple<string, long>(o, oid));
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

                            bool hasWork = outerCompanyConnection.QuerySingle<Boolean>(existsSql);

                            if (hasWork)
                            {

                                var checkoutAndGetQueueItemSql = $@"
declare @IDs table (ID uniqueidentifier)
insert into @IDs
select top {numberOfQueueItems} ID 
from [queue].[Task] 
where MachineAssigned is null and NumberOfRetries < 2  and [date] < DATEADD(minute, -1, getutcdate()) 
order by [Date] asc

update  T
set     T.MachineAssigned = @m
from    [queue].[Task] T
        inner join @IDs S on S.ID = T.ID

select  T.* 
from    [queue].[Task] T
        inner join @IDs S on S.ID = T.ID
";
                                var queueItems = outerCompanyConnection.Query<QueueTask>(checkoutAndGetQueueItemSql, new { m = new DbString { Value = System.Environment.MachineName, IsAnsi = true, Length = 250 } }).ToList();

                                queueItems.AsParallel().ForAll(q =>
                                {
                                    try
                                    {
                                        using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                                        {
                                            companyConnection.Open();

                                            try
                                            {
                                                switch (q.Action)
                                                {
                                                    case "Add":
                                                        #region
                                                        addAuditEntry(companyConnection, q.Object, q.ObjectID, "Created", q.Custom, q.AssetID);
                                                        resolveIndexItem(companyConnection, q.Object, q.ObjectID, "A", q.AssetID);
                                                        break;
                                                    #endregion
                                                    case "Delete":
                                                        #region                                     
                                                        if (IsValidTypeForAuditAction(q.Action, q.Object))
                                                        {
                                                            addAuditEntry(companyConnection, q.Object, q.ObjectID, "Removed", q.Custom, q.AssetID);
                                                        }
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

                                                        break;
                                                    #endregion
                                                    case "FusionCache":
                                                        #region
                                                        if (settings == null)
                                                        {
                                                            settings = CompanyConnectionUtils.GetCompanySettings(c.CompanyID);
                                                        }
                                                        bool useNewMarkitLineage = settings.Any(s => s.SettingID == markitLineageSettingID && s.Value.ToLower() == "true");
                                                        companyConnection.Execute("exec fusion.ProcessFusionCacheInQueue @FusionID, @useNewMarkitLineage", new { FusionID = q.ObjectID, useNewMarkitLineage }, null, 10800);    // 180 minute timeout.
                                                        break;
                                                    #endregion
                                                    case "Notify":
                                                        #region
                                                        switch (q.Object)
                                                        {
                                                            case "FusionExecution":
                                                                #region
                                                                var execution = companyConnection.Query<FusionExecution>(@"select * from fusion.Execution where ID = @id", new { id = q.ObjectID }, null, true, 900).FirstOrDefault();

                                                                if (execution != null)
                                                                {
                                                                    var fusionInfo = companyConnection.Query<dynamic>(Sql.FusionInfo, new { id = execution.FusionID }).FirstOrDefault();

                                                                    var resourcesToNotify = companyConnection.Query<dynamic>(Sql.FusionResources, new { id = execution.FusionID }, null, true, 900).ToList();

                                                                    resourcesToNotify.ForEach(r =>
                                                                    {
                                                                        var tags = new Dictionary<string, string>();
                                                                        tags.Add("user", r.Name);
                                                                        tags.Add("fusion", fusionInfo.Fusion);
                                                                        tags.Add("fusionType", fusionInfo.FusionType);
                                                                        tags.Add("adds", execution.Adds.HasValue ? execution.Adds.Value.ToString() : "None");
                                                                        tags.Add("updates", execution.Updates.HasValue ? execution.Updates.Value.ToString() : "None");
                                                                        tags.Add("deletes", execution.Deletes.HasValue ? execution.Deletes.Value.ToString() : "None");
                                                                        tags.Add("fusionUrl", $"https://{c.UrlPrefix}.data3sixty.com/fusion/{fusionInfo.FusionID}");
                                                                        tags.Add("executionUrl", $"https://{c.UrlPrefix}.data3sixty.com/fusion/history/{fusionInfo.FusionID}");
                                                                        tags.Add("startDate", execution.DateStarted.Value.ToShortDateString());
                                                                        tags.Add("startTime", execution.DateStarted.Value.ToShortTimeString());
                                                                        SendMailToUser(r.Name, r.Email, "Data3Sixty - Fusion Update Notification", "fusion-update-notification-immediate", tags, "Data3Sixty Fusion");
                                                                    });
                                                                }
                                                                break;
                                                                #endregion
                                                        }
                                                        break;
                                                    #endregion
                                                    case "ObjectIndex":
                                                        #region
                                                        resolveIndexItem(companyConnection, q.Object, q.ObjectID, q.Custom, q.AssetID);
                                                        break;
                                                    #endregion
                                                    case "Update":
                                                        #region
                                                        addAuditEntry(companyConnection, q.Object, q.ObjectID, "Updated", q.Custom, q.AssetID);

                                                        if (q.Object != "PolicyType" && q.Object != "TaxonomyType")
                                                            resolveIndexItem(companyConnection, q.Object, q.ObjectID, "U", q.AssetID);
                                                        break;
                                                    #endregion
                                                    case "TagConsolidated":
                                                        addAuditEntry(companyConnection, q.Object, q.ObjectID, "Tag Consolidate", q.Custom, q.AssetID);
                                                        break;
                                                    case "CompanySettingsUpdate":
                                                        addAuditEntry(companyConnection, q.Object, q.ObjectID, "Update settings", q.Custom, q.AssetID);

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

                        #region Now deal with INDEXING

                        try
                        {

                            var search = new ElasticSearchSource();

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
                            if (indexCollectionModel.UpsertByObject.Any() || indexCollectionModel.UpsertByUid.Any())
                            {
                                try
                                {
                                    using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                                    {
                                        companyConnection.Open();
                                        SearchIndexer indexer = new SearchIndexer(companyConnection, c.CompanyID, search);

                                        if (indexCollectionModel.UpsertByUid.Any())
                                            indexer.IndexAssets(indexCollectionModel.UpsertByUid);

                                        if (indexCollectionModel.UpsertByObject.Any())
                                            indexer.IndexAssets(indexCollectionModel.UpsertByObject);

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


        private static bool IsValidTypeForAuditAction(string action, string obj)
        {
            if ((action ?? "").ToUpper() == "DELETE")
            {
                if (obj == SystemObjects.Tag.ToString())
                    return true;
                else if ((obj ?? "").ToUpper() == "RESPONSIBILITYTYPERELATIONOVERRIDEITEM")
                    return true;
                return false;
            }
            return true;
        }

        private static bool ShouldItemBeIndexedForElasticSearch(string obj)
        {
            if (string.IsNullOrEmpty(obj)) return false;

            // ignore intersects we dont want to add them to the search index.
            if (string.Compare(obj, "IntersectType", true) == 0
                    || string.Compare(obj, "ResponsibilityType", true) == 0
                    || string.Compare(obj, "FusionAttributeType", true) == 0
                    || string.Compare(obj, "Lookup", true) == 0
                    || string.Compare(obj, "LookupType", true) == 0
                    || string.Compare(obj, "Tag", true) == 0
                    || string.Compare(obj, "FieldType", true) == 0
                    || string.Compare(obj, "ArtifactType", true) == 0
                    || string.Compare(obj, "IssueType", true) == 0
                    ) return false;

            return true;
        }

        private static void addAuditEntry(SqlConnection companyConnection, string @object, int objectID, string oper, string custom, long assetID)
        {
            if (!string.IsNullOrEmpty(custom))
            {
                var customXml = XElement.Parse(custom);

                //ActionObjectValue holds new value, as target is not in company table
                if (custom.Contains("ActionObjectValue"))
                {
                    companyConnection.Execute(
                    "exec [utility].[AddAuditEntry]  @ParentObject, @ParentObjectID, @ResourceID, @date, @op, @Object, @ObjectID, @NewValue",
                    new
                    {
                        Object = @object,
                        ObjectID = objectID,
                        ParentObject = customXml.Element("ActionObject").Value,
                        date = DateTime.UtcNow,
                        ParentObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                        ResourceID = int.Parse(customXml.Element("ResourceID").Value),
                        op = oper,
                        NewValue = customXml.Element("ActionObjectValue").Value
                    },
                    null,
                    600);
                }
                else
                {
                    companyConnection.Execute(
                            "exec [utility].[AddAuditEntry]  @ParentObject, @ParentObjectID, @ResourceID, @date, @op, @Object, @ObjectID",
                            new
                            {
                                Object = @object,
                                ObjectID = objectID,
                                ParentObject = customXml.Element("ActionObject").Value,
                                date = DateTime.UtcNow,
                                ParentObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                                ResourceID = int.Parse(customXml.Element("ResourceID").Value),
                                op = oper
                            },
                            null,
                            600);    // 5 minute timeout.
                }
            }
        }

    }

    internal class TagSqlModel
    {
        public Guid TagUID { get; set; }
        public string Value { get; set; }
    }

    internal class ResponsibilitySqlModel
    {
        public long AssetID { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
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
        }

        public ConcurrentBag<IndexObjectModel> Adds { get; set; }
        public ConcurrentBag<IndexObjectModel> Deletes { get; set; }
        public ConcurrentBag<IndexObjectModel> Updates { get; set; }
        public ConcurrentBag<Guid> UpsertByUid { get; set; }
        public ConcurrentBag<Tuple<string, long>> UpsertByObject { get; set; }
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

    public static class Sql
    {
        #region Notification Task : SQL Statements

        public static string Comment = @"
select	C.ID,
		C.Body,
		C.DateCreated,
		R.FirstName + ' ' + R.LastName as Author,
		C.ParentID,
		P.Body as ParentBody,
		P.DateCreated as ParentDateCreated,
		PR.FirstName + ' ' + PR.LastName as ParentAuthor,
		utility.GetAssetDisplayValueWrapper(D.ID) as OwnerName,
		dbo.GenerateAssetUrl(D.ID) as OwnerUrl,
		T.Name as OwnerTypeName,
		case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
		inner join Asset D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
		inner join AssetType T on T.ID = D.AssetTypeID
		left join Comment P on P.ID = C.ParentID
		left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where	(select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";


        public static string FusionResources = @"
select	R.ResourceID, 
        RE.FirstName + ' ' + RE.LastName as Name, 
        RE.Email 
from	ResponsibilityDetail R 
        inner join reporting.Global_Resource RE on RE.ResourceID = R.ResourceID and RE.Email not like '%?subject=%' 
where   R.Object = 'Fusion' and R.ObjectID = @id;";

        public static string FusionInfo = @"
select  F.ID as FusionID, 
        F.Name as Fusion, 
        FT.ID as FusionTypeID,  
        FT.Name as FusionType 
from    Fusion F 
        inner join FusionType FT on FT.ID = F.FusionTypeID and F.ID = @id";

        #endregion
    }


}
