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
using d360.core.queue;
using d360.extensions.queue;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Mandrill;
using Mandrill.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace igx.jobs.databasetaskprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class DatabaseTaskProcessor
    {
        public static void SendMailToUser(string toName, string toEmail, string subject, string body, string templateID, Dictionary<string, string> templateTags, string fromName = "Data3Sixty Workflow")
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = fromName;
            message.Subject = subject;

            message.TrackOpens = false;
            message.TrackClicks = false;

            var tags = new Dictionary<string, object>();
            if (templateTags != null)
            {
                foreach (var k in templateTags.Keys)
                {
                    message.AddRcptMergeVars(toEmail, k, templateTags[k]);
                }
            }

            //Add the HTML and Text bodies
            //message.Html = body;
            //message.Text = "Hello World plain text!"; 

            var api = new MandrillApi(constants.MANDRILL_API_KEY);
            var resp = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }

        const string functionName = "DatabaseTask_ProcessScheduled";
        const string timerSettings = "*/10 * * * * *";
        

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
                //companies = companies.Where(x => x.CompanyID == 4).ToList();

                companies.Shuffle(); //Randomize

                #region Must keep this segment here b/c this webjob can execute on multiple machines.  We do not want two or more machines trying to grab hold of the same queue item.
                var rand = new Random();
                int sleepSeconds = rand.Next(1, 10);
                Thread.Sleep(sleepSeconds * 500);
                #endregion

                companies.AsParallel().ForAll(c =>
                {
                    var numberOfQueueItems = 1000;
                    var indexCollectionModel = new ObjectIndexCollectionModel();

                    var outerCompanyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID);
                    outerCompanyConnection.OpenWithRetry(RetryPolicy.DefaultProgressive);

                    #region Indexer Func

                    Func<SqlConnection, string, int, string, string> resolveIndexItem = (companyConnection, o, oid, a) => {
                        ObjectDetail detail = null;
                        Dictionary<string, string> fields = new Dictionary<string, string>();

                        if (string.IsNullOrEmpty(o)) return "";

                        // ignore intersects we dont want to add them to the search index.
                        if (string.Compare(o, "IntersectType", true) == 0
                                || string.Compare(o, "Event", true) == 0
                                || string.Compare(o, "EventType", true) == 0
                                || string.Compare(o, "EventGroup", true) == 0
                                || string.Compare(o, "ResponsibilityType", true) == 0
                                || string.Compare(o, "FusionAttributeType", true) == 0
                                || string.Compare(o, "Intersect", true) == 0
                                || string.Compare(o, "Lookup", true) == 0
                                || string.Compare(o, "LookupType", true) == 0
                                ) return "";

                        #region Load Info for Object

                        detail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = o, i = oid }).SingleOrDefault();
                        var fldInfo = companyConnection.Query<FieldWithRelation>(
                            "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                            new { t = new Dapper.DbString { Value = o.ToString(), IsAnsi = true }, i = oid }
                            );
                            
                        if(fldInfo != null)
                            fields = fldInfo.ToDictionary(k => k.Name, v => v.FormattedValue);

                        var itemUrl = detail != null ? detail.Url : "";
                        var itemTypeName = detail != null ? detail.TypeName : "";
                        var itemName = detail != null ? detail.Name : "";
                        var itemParentType = detail != null ? detail.ParentType : "";
                        var itemParentId = detail != null ? (detail.ParentID ?? 0) : 0;

                        //if the item 
                        var assetSql = "select id from asset where [object] = @obj and [objectID] = @i";

                        long assetId = 0;

                        if (detail != null)
                        {
                            if (o == "Artifact")
                            {
                                var assetIDResult = companyConnection.Query<long?>(assetSql, new { @obj = o, i = oid }).FirstOrDefault();
                                if (assetIDResult.HasValue)
                                    assetId = assetIDResult.Value;
                            }

                            if (fields.ContainsKey("Name")) fields["Name"] = detail.Name;
                            else fields.Add("Name", detail.Name);

                            if (o == "Synonym")
                            {
                                fields.Add("SynonymFor", detail.TextPath);
                                fields.Add("SynonymForObject", detail.ParentType);
                                fields.Add("SynonymForObjectType", detail.Description);
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(detail.Description))
                                {
                                    if (fields.ContainsKey("Description"))
                                        fields["Description"] = detail.Description;
                                    else
                                        fields.Add("Description", detail.Description);
                                }

                                if (fields.ContainsKey("TextPath")) fields["TextPath"] = detail.TextPath;
                                else fields.Add("TextPath", detail.TextPath);

                                if (fields.ContainsKey("Type")) fields["Type"] = detail.TypeName;
                                else fields.Add("Type", detail.TypeName);
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
	                                        ,dbo.GenerateObjectUrl(s.[Object], c.ObjectTypeID, s.[ObjectID]) as 'Url'
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
                                itemName = nymRecord.Synonym;

                                var nymDetail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = nymRecord.SynonymForObject, i = nymRecord.SynonymForObjectID }).SingleOrDefault();


                                fields.Add("NymType", nymRecord.PredicateName);
                                fields.Add("Name", nymRecord.Synonym);
                                fields.Add("SynonymFor", nymRecord.SynonymFor);
                                fields.Add("SynonymForObject", nymRecord.SynonymForObject);
                                fields.Add("SynonymForObjectType", nymRecord.SynonymForObjectType);

                                itemParentId = nymRecord.SynonymForObjectID;
                                itemParentType = nymRecord.PredicateName;
                                itemUrl = nymDetail.Url;
                            }
                        }

                        #endregion

                        switch (a)
                        {
                            case "A":   //Add
                                var add = new AddToIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = itemUrl, To = QueueAction.AddToIndex, Type = itemTypeName };
                                if (o == "Synonym")
                                {
                                    add.ItemUniqueID = $"custom|{itemName}|{itemParentType}|{itemParentId}";
                                }
                                else if (o == "Artifact" && assetId > 0)
                                {
                                    add.ItemUniqueID = assetId.ToString();                                    
                                }
                                indexCollectionModel.Adds.Add(add);
                                break;
                            case "U":   //Update
                                var update = new UpdateInIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = itemUrl, To = QueueAction.UpdateInIndex, Type = itemTypeName };
                                if (o == "Synonym")
                                {
                                    update.ItemUniqueID = $"custom|{itemName}|{itemParentType}|{itemParentId}";
                                }
                                else if (o == "Artifact" && assetId > 0)
                                {
                                     update.ItemUniqueID = assetId.ToString();                                    
                                }
                                indexCollectionModel.Updates.Add(update);
                                break;
                            case "D":   //Delete
                                var delete = new RemoveFromIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = "#", To = QueueAction.RemoveFromIndex }; //, Type = detail.TypeName                                
                                if (o == "Artifact" && assetId > 0) delete.ItemUniqueID = assetId.ToString();
                                indexCollectionModel.Deletes.Add(delete);
                                break;
                        }

                        return "";
                    };

                    #endregion

                    var total = outerCompanyConnection.Query<int>("select count(1) from [queue].[Task] where MachineAssigned is null and NumberOfRetries < 2").Single();
                    var checkoutAndGetQueueItemSql = $@"
declare @IDs table (ID uniqueidentifier)
insert into @IDs
select top {numberOfQueueItems} ID 
from [queue].[Task] 
where MachineAssigned is null and NumberOfRetries < 2 
order by [Priority] asc, [Date] asc

update  T
set     T.MachineAssigned = @m
from    [queue].[Task] T
        inner join @IDs S on S.ID = T.ID

select  T.* 
from    [queue].[Task] T
        inner join @IDs S on S.ID = T.ID
";
                    var queueItems = outerCompanyConnection.Query<QueueTask>(checkoutAndGetQueueItemSql, new { m = System.Environment.MachineName }).ToList();

                    queueItems.AsParallel().ForAll(q =>
                    {
                        var companyConnection = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID);
                        companyConnection.Open();

                        try
                        {
                            switch (q.Action)
                            {
                                case "Add":
                                    #region
                                    addAuditEntry(companyConnection, q.Object, q.ObjectID, "Created", q.Custom);

                                    resolveIndexItem(companyConnection, q.Object, q.ObjectID, "A");
                                    break;
                                #endregion
                                case "Delete":
                                    #region
                                    resolveIndexItem(companyConnection, q.Object, q.ObjectID, "D");
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
                                case "FollowChildren":
                                    #region
                                    switch (q.Object)
                                    {
                                        case "Taxonomy":
                                            var processItems = companyConnection.Query<int>(Sql.TaxonomyParents, new { id = (int)q.ObjectID });
                                            foreach (var item in processItems)
                                            {
                                                companyConnection.Execute("SetChildrenByFollowID @id", new { id = (int)item }, null, 180); //3 minute timeout
                                            }
                                            break;
                                    }

                                    //cleanup orphaned FollowChild records
                                    companyConnection.Execute("delete from followchild where not exists(select * from follow f where f.followtypeid = 3 and f.objecttype = parentobjecttype and f.objectid = parentobjectid)", null, null, 500);
                                    break;
                                #endregion
                                case "FusionCache":
                                    #region
                                    companyConnection.Execute("exec fusion.ProcessFusionCacheInQueue @FusionID", new { FusionID = q.ObjectID }, null, 10800);    // 180 minute timeout.
                                    break;
                                #endregion
                                case "Notify":
                                    #region
                                    switch (q.Object)
                                    {
                                        case "Comment":
                                            #region
                                            var comment = companyConnection.Query<CommentInfo>(Sql.Comment, new { CommentID = q.ObjectID }, null, true, 900).FirstOrDefault();

                                            if (comment != null)
                                            {
                                                var resourcesToNotify = companyConnection.Query<CommentNotificationUser>(Sql.Resources, new { CommentID = q.ObjectID }, null, true, 900).ToList();

                                                resourcesToNotify.ForEach(r => {
                                                    var tags = new Dictionary<string, string>();
                                                    tags.Add("user", r.Name);
                                                    tags.Add("author", comment.Author);
                                                    tags.Add("origin", comment.OriginationType);

                                                    tags.Add("ownerName", comment.OwnerName);
                                                    tags.Add("ownerType", comment.OwnerTypeName);
                                                    tags.Add("body", comment.Body);
                                                    tags.Add("ownerUrl", $"https://{c.UrlPrefix}.data3sixty.com/{comment.OwnerUrl}");
                                                    var parentReference = "";
                                                    if (comment.ParentID.HasValue)
                                                    {
                                                        parentReference = string.Format("<p>This is a reply to the original comment by {0} on {1}</p><p style=\"margin-top: 10px; margin-bottom: 10px; border: 1px solid #3979a2; background-color:#eeeeee\">{2}</p>", comment.ParentAuthor, comment.ParentDateCreated, comment.ParentBody);
                                                    }
                                                    tags.Add("parentReference", parentReference);
                                                    tags.Add("createDate", comment.DateCreated.ToShortDateString());
                                                    SendMailToUser(r.Name, r.Email, "Data3Sixty - New Comment Added", "", "comment-notification-immediate", tags, "Data3Sixty Community");
                                                });
                                            }
                                            break;
                                        #endregion
                                        case "FusionExecution":
                                            #region
                                            var execution = companyConnection.Query<FusionExecution>(@"select * from fusion.Execution where ID = @id", new { id = q.ObjectID }, null, true, 900).FirstOrDefault();

                                            if (execution != null)
                                            {
                                                var fusionInfo = companyConnection.Query<dynamic>(Sql.FusionInfo, new { id = execution.FusionID }).FirstOrDefault();

                                                var resourcesToNotify = companyConnection.Query<dynamic>(Sql.FusionResources, new { id = execution.FusionID }, null, true, 900).ToList();

                                                resourcesToNotify.ForEach(r => {
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
                                                    SendMailToUser(r.Name, r.Email, "Data3Sixty - Fusion Update Notification", "", "fusion-update-notification-immediate", tags, "Data3Sixty Fusion");
                                                });
                                            }
                                            break;
                                            #endregion
                                    }
                                    break;
                                #endregion
                                case "ObjectIndex":
                                    #region
                                    resolveIndexItem(companyConnection, q.Object, q.ObjectID, q.Custom);
                                    break;
                                #endregion
                                case "Update":
                                    #region
                                    addAuditEntry(companyConnection, q.Object, q.ObjectID, "Update", q.Custom);

                                    if(q.Object != "PolicyType" && q.Object != "TaxonomyType")
                                        resolveIndexItem(companyConnection, q.Object, q.ObjectID, "U");
                                    break;
                                    #endregion
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
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            }
                        }

                        companyConnection.Close();
                        companyConnection.Dispose();
                    });

                    outerCompanyConnection.Close();
                    outerCompanyConnection.Dispose();

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
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
        }

        private static void addAuditEntry(SqlConnection companyConnection, string @object, int objectID, string oper, string custom)
        {
            if (!string.IsNullOrEmpty(custom))
            {
                var customXml = XElement.Parse(custom);

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
