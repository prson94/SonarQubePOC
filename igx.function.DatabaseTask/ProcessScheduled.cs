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
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace igx.function.DatabaseTask
{
    #region Helper Classes

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
            Adds = new List<AddToIndexModel>();
            Deletes = new List<RemoveFromIndexModel>();
            Updates = new List<UpdateInIndexModel>();
        }

        public List<AddToIndexModel> Adds { get; set; }
        public List<RemoveFromIndexModel> Deletes { get; set; }
        public List<UpdateInIndexModel> Updates { get; set; }
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
		dbo.GenerateNgObjectUrl(T.Object, T.ObjectID,D.ObjectID) as OwnerUrl,
		T.Name as OwnerTypeName,
		case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
		inner join Asset D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
		inner join AssetType T on T.ID = D.AssetTypeID
		left join Comment P on P.ID = C.ParentID
		left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where	(select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";

        public static string Resources = @"select	F.ResourceID,
R.FirstName + ' ' + R.LastName as Name,
R.Email
from	CommentRelation CR
inner join FollowDetail F on F.ObjectType = CR.ObjectType and F.ObjectID = CR.ObjectID  and CR.CommentID = @CommentID
inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID and R.Email not like '%?subject=%'
union
select	coalesce(RG.ResourceID, R.ResponsibleObjectID) as ResourceID,
RE.FirstName + ' ' + RE.LastName as Name,
RE.Email
from	CommentRelation CR
inner join ResponsibilityDetail R on R.ObjectType = CR.ObjectType and R.ObjectID = CR.ObjectID and CR.CommentID = @CommentID
left join ResourceGroup RG on R.ResponsibleObjectType = 'Group' and RG.GroupID = R.ResponsibleObjectID
inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'";

        public static string FusionResources = @"
select	R.ResourceID, 
        RE.FirstName + ' ' + RE.LastName as Name, 
        RE.Email 
from	ResponsibilityDetails R on R.Object = 'Fusion' and R.ObjectID = @id 
        inner join reporting.Global_Resource RE on RE.ResourceID = R.ResourceID and RE.Email not like '%?subject=%'";

        public static string FusionInfo = @"
select  F.ID as FusionID, 
        F.Name as Fusion, 
        FT.ID as FusionTypeID,  
        FT.Name as FusionType 
from    Fusion F 
        inner join FusionType FT on FT.ID = F.FusionTypeID and F.ID = @id";

        #endregion

        #region Follow Children : SQL Statements

        public static string TaxonomyParents = @"with t as
                                            (
	                                            select t1.* from taxonomy t1 where t1.id = @id
	                                            union all
	                                            select t2.* from t
	                                            join taxonomy t2 on t2.id = t.parentid
                                            )
                                            select c.id from t 
                                            inner join FollowWithChildren c on c.objectid = t.id and c.objecttype = 'Taxonomy' and c.FollowTypeID = 3";

        #endregion

        #region Style Cache; SQL Statements

        public static string StyleCache = @"
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
inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.[Object] = S.ObjectType and T.ObjectID = S.ObjectID;";

        #endregion
    }

    public class CommentInfo
    {
        public int ID { get; set; }
        public string Body { get; set; }
        public DateTime DateCreated { get; set; }
        public string Author { get; set; }
        public int? ParentID { get; set; }
        public string ParentBody { get; set; }
        public DateTime? ParentDateCreated { get; set; }
        public string ParentAuthor { get; set; }
        public string OwnerName { get; set; }
        public string OwnerUrl { get; set; }
        public string OwnerTypeName { get; set; }
        public string OriginationType { get; set; }
    }
    public class CommentNotificationUser
    {
        public int ResourceID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    #endregion

    public static class ProcessScheduled
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

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log)
        {
            try
            {
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

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
                        Dictionary<string, string> fields = null;

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
                        fields = companyConnection.Query<FieldWithRelation>(
                            "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                            new { t = new Dapper.DbString { Value = o.ToString(), IsAnsi = true }, i = oid }
                            ).ToDictionary(k => k.Name, v => v.FormattedValue);

                        if (detail != null)
                        {
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
                                if (fields.ContainsKey("Description")) fields["Description"] = detail.Description;
                                else fields.Add("Description", detail.Description);

                                if (fields.ContainsKey("TextPath")) fields["TextPath"] = detail.TextPath;
                                else fields.Add("TextPath", detail.TextPath);

                                if (fields.ContainsKey("Type")) fields["Type"] = detail.TypeName;
                                else fields.Add("Type", detail.TypeName);
                            }
                        }

                        #endregion

                        switch (a)
                        {
                            case "A":   //Add
                                var add = new AddToIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.AddToIndex, Type = detail.TypeName };
                                if (o == "Synonym")
                                {
                                    add.ItemUniqueID = $"custom|{detail.Name}|{detail.ParentType}|{detail.ParentID.Value}";
                                }
                                indexCollectionModel.Adds.Add(add);
                                break;
                            case "U":   //Update
                                var update = new UpdateInIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.UpdateInIndex, Type = detail.TypeName };
                                if (o == "Synonym")
                                {
                                    update.ItemUniqueID = $"custom|{detail.Name}|{detail.ParentType}|{detail.ParentID.Value}";
                                }
                                indexCollectionModel.Updates.Add(update);
                                break;
                            case "D":   //Delete
                                var delete = new RemoveFromIndexModel { CompanyID = c.CompanyID, Fields = fields, Group = o, ID = oid, RelativeUrl = "#", To = QueueAction.RemoveFromIndex }; //, Type = detail.TypeName
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
                                case "Analytic":
                                #region
                                    companyConnection.Execute("exec utility.CalculateStatistics @Type, @ID", new { Type = q.Object, ID = q.ObjectID }, null, 180);    // 3 minute timeout.
                                break;
                                #endregion
                                case "Add":
                                #region
                                    if (!string.IsNullOrEmpty(q.Custom))
                                    {
                                        var customXml = XElement.Parse(q.Custom);
                                        companyConnection.Execute(
                                            "exec AsyncAddObject @Object, @ObjectID, @ParentObject, @ParentObjectID, @ResourceID",
                                            new
                                            {
                                                q.Object,
                                                q.ObjectID,
                                                ParentObject = customXml.Element("ActionObject").Value,
                                                ParentObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                                                ResourceID = int.Parse(customXml.Element("ResourceID").Value)
                                            },
                                            null,
                                            10800);    // 180 minute timeout.

                                                resolveIndexItem(companyConnection, q.Object, q.ObjectID, "A");
                                    }
                                break;
                                #endregion
                                case "Delete":
                                #region
                                    if (!string.IsNullOrEmpty(q.Custom))
                                    {
                                        var customXml = XElement.Parse(q.Custom);
                                        companyConnection.Execute(
                                            "exec AsyncDeleteObject @Object, @ObjectID, @ParentObject, @ParentObjectID, @ResourceID",
                                            new
                                            {
                                                q.Object,
                                                q.ObjectID,
                                                ParentObject = customXml.Element("ActionObject").Value,
                                                ParentObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                                                ResourceID = int.Parse(customXml.Element("ResourceID").Value)
                                            },
                                            null,
                                            10800);    // 180 minute timeout.

                                                resolveIndexItem(companyConnection, q.Object, q.ObjectID, "D");
                                    }
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
                                                    throw new ApplicationException("Unable to identify the Object specified."); }
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
                                case "ObjectCache":
                                #region
                                    companyConnection.Execute("exec cache.SynchronizeObjectDetails @type, @id", new { type = q.Object, id = q.ObjectID }, null, 180);
                                break;
                                #endregion
                                case "ObjectIndex":
                                #region
                                    resolveIndexItem(companyConnection, q.Object, q.ObjectID, q.Custom);
                                break;
                                #endregion
                                case "ObjectStyleCache":
                                #region
                                    companyConnection.Execute(Sql.StyleCache, new { type = q.Object, id = q.ObjectID }, null, 7200);
                                break;
                                #endregion
                                case "ObjectVersion":
                                #region
                                    if (!string.IsNullOrEmpty(q.Custom))
                                    {
                                        var customXml = XElement.Parse(q.Custom);
                                        companyConnection.Execute(
                                            "EXEC utility.AddAuditEntry @Object, @ObjectID, @ResourceID, @Date, @Action, @ActionObject, @ActionObjectID",
                                            new
                                            {
                                                q.Object,
                                                q.ObjectID,
                                                ResourceID = int.Parse(customXml.Element("ResourceID").Value),
                                                q.Date,
                                                Action = customXml.Element("Action").Value,
                                                ActionObject = customXml.Element("ActionObject").Value,
                                                ActionObjectID = int.Parse(customXml.Element("ActionObjectID").Value)
                                            },
                                            null,
                                            7200);
                                    }
                                break;
                                #endregion
                                case "Update":
                                #region
                                    if (!string.IsNullOrEmpty(q.Custom))
                                    {
                                        var customXml = XElement.Parse(q.Custom);
                                        companyConnection.Execute(
                                            "exec AsyncUpdateObject @Object, @ObjectID, @ParentObject, @ParentObjectID, @ResourceID",
                                            new
                                            {
                                                q.Object,
                                                q.ObjectID,
                                                ParentObject = customXml.Element("ActionObject").Value,
                                                ParentObjectID = int.Parse(customXml.Element("ActionObjectID").Value),
                                                ResourceID = int.Parse(customXml.Element("ResourceID").Value)
                                            },
                                            null,
                                            10800);    // 180 minute timeout.

                                                resolveIndexItem(companyConnection, q.Object, q.ObjectID, "U");
                                    }
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
    }
}
