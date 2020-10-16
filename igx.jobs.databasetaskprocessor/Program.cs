using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions.queue;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Mandrill;
using Mandrill.Model;
using Microsoft.Azure.WebJobs;

namespace igx.jobs.databasetaskprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif
            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class DatabaseTaskProcessor
    {
        public static void SendMailToUser(string toName, string toEmail, string subject, string templateID, Dictionary<string, string> templateTags, string fromName = "Data3Sixty Workflow")
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
            var api = new MandrillApi(CoreFunction.GetConfigValueByKey("MandrillApiKey"));
            var resp = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }

        const string functionName = "DatabaseTask_ProcessScheduled";
        const string timerSettings = "*/1 * * * * *";
        const int markitLineageSettingID = 62;


        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
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
                                // check if this object requires to go into elastic search
                                if (!ShouldItemBeIndexedForElasticSearch(o)) return string.Empty;

                                ObjectDetail detail = null;
                                IndexObjectModel indexObject = new IndexObjectModel {
                                    CompanyID = c.CompanyID,
                                    Fields = new Dictionary<string, string>(),
                                    Category = o,
                                    ID = oid,
                                    Tags = new Dictionary<string, string>()
                                };
                                if (givenAssetId > 0)
                                    indexObject.AssetID = givenAssetId;

                                //Set uniqueID for index object
                                if(o == "Synonym")
                                {
                                    indexObject.ItemUniqueID = $"custom|{oid}";
                                } else if (o == "Artifact" && indexObject.AssetID > 0)
                                {
                                    indexObject.ItemUniqueID = indexObject.AssetID.ToString();
                                }

                                #region Load Info for Object

                                //Only "Synonym' intersects should be indexed
                                if (o == "Intersect")
                                {
                                    //If Intersect does not have an assetID it should not be indexed
                                    if( givenAssetId > 0) return string.Empty;

                                    var sql = @"SELECT * FROM (" + ElasticSearchSource.INTERSECT_SYNONYM_QUERY +
                                        ") q WHERE q.ID = @oid AND q.SynonymAssetID = @givenAssetId";
                                    dynamic intersectDetail = companyConnection.Query<dynamic>(sql, new { oid, givenAssetId }).SingleOrDefault();

                                    //If Intersect does not have synonym details it should not be indexed
                                    if (intersectDetail == null) return string.Empty;

                                    indexObject.Category = "Synonym";
                                    indexObject.AssetType = "Synonym";
                                    indexObject.ItemUniqueID = $"intersect|{oid}|{intersectDetail.Direction}";
                                    indexObject.RelativeUrl = intersectDetail.Url;
                                    indexObject.Fields.Add("Name", intersectDetail.Synonym);
                                    indexObject.Fields.Add("NymType", intersectDetail.PredicateName);
                                    indexObject.Fields.Add("SynonymFor", intersectDetail.SynonymFor);
                                    indexObject.Fields.Add("SynonymForObject", intersectDetail.SynonymForObject);
                                    indexObject.Fields.Add("SynonymForObjectType", intersectDetail.SynonymForObjectType);
                                }
                                else
                                {

                                    detail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = o, i = oid }).SingleOrDefault();

                                    var fldInfo = companyConnection.Query<FieldWithRelation>(
                                        "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                                        new { t = new Dapper.DbString { Value = o.ToString(), IsAnsi = true }, i = oid }
                                    );

                                    if (fldInfo != null)
                                        indexObject.Fields = fldInfo.ToDictionary(k => k.Name, v => v.FormattedValue);

                                    indexObject.RelativeUrl = detail != null ? detail.Url : "";
                                    indexObject.AssetType = detail != null ? detail.TypeName : "";

                                    if (detail != null)
                                    {
                                        indexObject.Category = (o == SystemObjects.Artifact.ToString()) ? detail.Class.ToString() : o;

                                        if (indexObject.Fields.ContainsKey("Name")) indexObject.Fields["Name"] = detail.Name;
                                        else indexObject.Fields.Add("Name", detail.Name);

                                        if (detail.AssetTypeUid.HasValue)
                                        {
                                            indexObject.AssetTypeUid = detail.AssetTypeUid.Value;
                                        }

                                        if (o == "Synonym")
                                        {
                                            indexObject.Fields.Add("SynonymFor", detail.TextPath);
                                            indexObject.Fields.Add("SynonymForObject", detail.ParentType);
                                            indexObject.Fields.Add("SynonymForObjectType", detail.Description);
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(detail.Description))
                                            {
                                                if (indexObject.Fields.ContainsKey("Description")) indexObject.Fields["Description"] = detail.Description;
                                                else indexObject.Fields.Add("Description", detail.Description);
                                            }

                                            if (indexObject.Fields.ContainsKey("TextPath")) indexObject.Fields["TextPath"] = detail.TextPath;
                                            else indexObject.Fields.Add("TextPath", detail.TextPath);

                                            indexObject.AssetType = detail.TypeName;
                                            indexObject.Uid = detail.UID;
                                        }

                                        if (indexObject.AssetID > 0)
                                        {
                                            indexObject.Tags = companyConnection
                                                .Query<TagSqlModel>("SELECT t.uid AS TagUID, t.Value FROM [dbo].[AssetTag] at INNER JOIN [dbo].[Tag] t ON at.TagID = t.ID WHERE at.AssetID = @i", new { i = indexObject.AssetID })
                                                .ToDictionary(x => x.TagUID.ToString(), x => x.Value);

                                            IEnumerable<ResponsibilitySqlModel> secset = companyConnection
                                                .Query<ResponsibilitySqlModel>("SELECT * FROM (" + ElasticSearchSource.GetAssetResponsibilityQuery() + ") q WHERE q.AssetID = @i", new { i = indexObject.AssetID });

                                            indexObject.NoRead = new Dictionary<string, List<int>> {
                                                { "R" , secset.Where(r => r.SecurityAsset == "R").Select(r => r.SecurityAssetID).ToList() },
                                                { "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() },
                                                { "O" , secset.Where(r => r.SecurityAsset == "O").Select(r => r.SecurityAssetID).ToList() }
                };
                                        }
                                    }
                                    else if ((detail == null) && (string.Compare(o, "Synonym", true) == 0))
                                    {
                                        var sql = @"
                                        select 
	                                        s.Name as 'Synonym'
	                                        ,c.Name as 'SynonymFor'
	                                        ,s.[Object] as 'SynonymForObject'
	                                        ,s.[ObjectID] as 'SynonymForObjectID'
	                                        ,dbo.GenerateObjectUrl(s.[Object], c.ObjectTypeID, s.[ObjectID], 0x0, 0) as 'Url'
	                                        ,c.ObjectTypeName as 'SynonymForObjectType'	
                                            ,p.Name as 'PredicateName'    
                                            ,s.ID as 'ID'                
                                        from
	                                        [dbo].[nym] s
	                                        inner join [cache].[objectdetails] c on (s.[Object] = c.[Object] and s.[ObjectID] = c.[ObjectID])
                                            inner join [dbo].[predicate] p on (s.predicateid = p.id) where s.id = @id";

                                        //custom synonym load details from nym table
                                        var nymRecord = companyConnection.Query<dynamic>(sql, new { id = oid }).FirstOrDefault();

                                        if (nymRecord != null)
                                        {
                                            var nymDetail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = nymRecord.SynonymForObject, i = nymRecord.SynonymForObjectID }).SingleOrDefault();

                                            indexObject.Fields.Add("NymType", nymRecord.PredicateName);
                                            indexObject.Fields.Add("Name", nymRecord.Synonym);
                                            indexObject.Fields.Add("SynonymFor", nymRecord.SynonymFor);
                                            indexObject.Fields.Add("SynonymForObject", nymRecord.SynonymForObject);
                                            indexObject.Fields.Add("SynonymForObjectType", nymRecord.SynonymForObjectType);

                                            indexObject.RelativeUrl = nymDetail.Url;
                                        }
                                    }
                                }

                                #endregion

                                switch (a)
                                {
                                    case "A":   //Add
                                        indexObject.To = QueueAction.AddToIndex;

                                       if (o == "Resource" && oid> 0)
                                        {
                                            dynamic userDetail = companyConnection.Query<dynamic>(@"SELECT Email,
                                                CASE
                                                WHEN Email not like '%@data3sixty.com' and Email not like '%@infogix.com'
                                                    THEN '0'
                                                    ELSE '1'
                                                END as Data3SixtyUser
                                                FROM reporting.global_resource
                                                WHERE ResourceID = @oid", new { oid }).SingleOrDefault();

                                            if (userDetail != null)
                                            {
                                                if (indexObject.Fields.ContainsKey("Email")) indexObject.Fields["Email"] = userDetail.Email;
                                                else indexObject.Fields.Add("Email", userDetail.Email);

                                                if (indexObject.Fields.ContainsKey("Data3SixtyUser")) indexObject.Fields["Data3SixtyUser"] = userDetail.Data3SixtyUser;
                                                else indexObject.Fields.Add("Data3SixtyUser", userDetail.Data3SixtyUser);
                                            }
                                        }
                                        indexCollectionModel.Adds.Add(indexObject);
                                        break;
                                    case "U":   //Update
                                        indexObject.To = QueueAction.UpdateInIndex;
                                        indexCollectionModel.Updates.Add(indexObject);
                                        break;
                                    case "D":   //Delete
                                        indexObject.To = QueueAction.RemoveFromIndex;
                                        indexObject.RelativeUrl = "#";

                                        //Intersects have two search documents, se we need to delete both
                                        if (o == "Intersect")
                                        {
                                            indexObject.Category = "Synonym";
                                            indexObject.AssetType = "Synonym";

                                            IndexObjectModel reciprocal = indexObject.ShallowCopy();
                                            reciprocal.ItemUniqueID  = $"intersect|{oid}|O";
                                            indexObject.ItemUniqueID = $"intersect|{oid}|S";
                                            indexCollectionModel.Deletes.Add(reciprocal);
                                        }

                                        indexCollectionModel.Deletes.Add(indexObject);
                                        break;
                                }

                                return "";
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
                                                    if(IsValidTypeForAuditAction(q.Action,q.Object))                                                    
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
            if ((action ?? "").ToUpper() == "DELETE") {
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
                    || string.Compare(obj, "Task", true) == 0
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
}
