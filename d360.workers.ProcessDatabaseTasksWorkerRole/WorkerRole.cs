using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using d360.core;
using d360.utils.company;
using d360.core.entities;
using d360.core.queue;
using Dapper;
using System.Xml.Linq;
using Mandrill.Model;
using Mandrill;
using System.Data.SqlClient;
using d360.extensions;

namespace d360.workers.ProcessDatabaseTasksWorkerRole
{
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

    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

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
            api.Messages.SendTemplate(message, templateID);

            message = null;
            api = null;
        }

        public override void Run()
        {
            Trace.TraceInformation("d360.workers.ProcessDatabaseTasksWorkerRole is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("d360.workers.ProcessDatabaseTasksWorkerRole has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("d360.workers.ProcessDatabaseTasksWorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("d360.workers.ProcessDatabaseTasksWorkerRole has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var checkDelayInSeconds = 10;
                    
                    await Task.Run(delegate {
                        //var companies = new List<int>() { 4 };
                        var companies = CompanyConnectionUtils.GetActiveCompanyIDs();
                        var domainPrefixes = CompanyConnectionUtils.GetCompanyDomainPrefixes();

                        companies.Shuffle(); //Randomize


                        #region Must keep this segment here b/c this webjob can execute on multiple machines.  We do not want two or more machines trying to grab hold of the same queue item.
                        var rand = new Random();
                        int sleepSeconds = rand.Next(1, 10);
                        Thread.Sleep(sleepSeconds * 500);
                        #endregion

                        //companies.ForEach(companyID =>
                        companies.AsParallel().ForAll(companyID =>
                        {
                            var isCompanyInDev = CompanyConnectionUtils.IsCompanyDevelopmentEnvironment(companyID);

                            var numberOfQueueItems = 1000;
                            var indexCollectionModel = new ObjectIndexCollectionModel();

                            var outerCompanyConnection = CompanyConnectionUtils.GetCompanyConnection(companyID);
                            outerCompanyConnection.Open();

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
                                        var add = new AddToIndexModel { CompanyID = companyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.AddToIndex, Type = detail.TypeName };
                                        if (o == "Synonym")
                                        {
                                            add.ItemUniqueID = $"custom|{detail.Name}|{detail.ParentType}|{detail.ParentID.Value}";
                                        }
                                        indexCollectionModel.Adds.Add(add);
                                        break;
                                    case "U":   //Update
                                        var update = new UpdateInIndexModel { CompanyID = companyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.UpdateInIndex, Type = detail.TypeName };
                                        if (o == "Synonym")
                                        {
                                            update.ItemUniqueID = $"custom|{detail.Name}|{detail.ParentType}|{detail.ParentID.Value}";
                                        }
                                        indexCollectionModel.Updates.Add(update);
                                        break;
                                    case "D":   //Delete
                                        var delete = new RemoveFromIndexModel { CompanyID = companyID, Fields = fields, Group = o, ID = oid, RelativeUrl = "#", To = QueueAction.RemoveFromIndex }; //, Type = detail.TypeName
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

                            if (total > numberOfQueueItems)
                            {
                                checkDelayInSeconds = 0;
                            }

                            //queueItems.ForEach(q =>
                            //{
                            //    companyConnection.Execute("update [queue].[Task] set MachineAssigned = @m where ID = @queueID", new { m = System.Environment.MachineName, queueID = q.ID });
                            //});

                            queueItems
                            .AsParallel().ForAll(q =>
                            //.ForEach(q =>
                            {
                                var companyConnection = CompanyConnectionUtils.GetCompanyConnection(companyID);
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
                                            var domainPrefix = domainPrefixes.First(i => i.Key == companyID).Value;

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
                                                            tags.Add("ownerUrl", $"https://{domainPrefix}.data3sixty.com/{comment.OwnerUrl}");
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
                                                            tags.Add("fusionUrl", $"https://{domainPrefix}.data3sixty.com/fusion/{fusionInfo.FusionID}");
                                                            tags.Add("executionUrl", $"https://{domainPrefix}.data3sixty.com/fusion/{fusionInfo.FusionID}?execution={execution.ID}");
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
                                    }

                                    companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                    Debug.WriteLine($"Completed for: Company {companyID}, ID {q.ID}");
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError(ex.GetFullExceptionData());
                                    try
                                    {
                                        if (q.NumberOfRetries >= 2)
                                            companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                        else
                                            companyConnection.Execute(@"update [queue].[Task] set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = true }, null, 500);
                                    }
                                    catch (Exception iex)
                                    {
                                        Trace.TraceError(iex.GetFullExceptionData());
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

                                ISearchSource search = null;
                                //if (isCompanyInDev)
                                    search = new extensions.search.ElasticSearchSource();
                                //else
                                //    search = new extensions.search.AzureSearchSource();

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
                                var msg = ex.GetFullExceptionData();
                                Trace.TraceError(msg);
                            }

                            #endregion
                        });

                        Thread.Sleep(checkDelayInSeconds * 1000);
                    });
                }
                catch (Exception ex)
                {
                    var msg = ex.GetFullExceptionData();
                    Trace.TraceError(msg);
                }
            }
        }
    }
}
