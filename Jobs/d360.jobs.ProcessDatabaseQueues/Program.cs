using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Threading;
using Dapper;
using d360.core.entities;
using d360.core.queue;
using System.Xml.Linq;

namespace d360.jobs.ProcessDatabaseQueues
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


    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                bool shouldRunAgain = true;

                //var companies = new List<int>() { 22 };
                var companies = GetActiveCompanyIDs();
                var domainPrefixes = GetCompanyDomainPrefixes();

                while (shouldRunAgain)
                {
                    shouldRunAgain = false;

                    #region Must keep this segment here b/c this webjob can execute on multiple machines.  We do not want two or more machines trying to grab hold of the same queue item.
                    var rand = new Random();
                    int sleepSeconds = rand.Next(1, 10);
                    Thread.Sleep(sleepSeconds * 500);
                    #endregion

                    companies.Shuffle(); //Randomize

                    companies.ForEach(companyID => //.AsParallel().WithDegreeOfParallelism(10).ForAll(companyID =>
                    {
                        var numberOfQueueItems = 100;
                        var indexCollectionModel = new ObjectIndexCollectionModel();

                        var companyConnection = GetCompanyConnection(companyID);
                        companyConnection.Open();

                        #region Indexer Func

                        Func<string, int, string, string> resolveIndexItem = (o, oid, a) => {
                            ObjectDetail detail = null;
                            Dictionary<string, string> fields = null;

                            #region Load Info for Object

                            detail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = o, i = oid }).SingleOrDefault();
                            fields = companyConnection.Query<FieldWithRelation>(
                                "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                                new { t = o, i = oid }
                                ).ToDictionary(k => k.Name, v => v.FormattedValue);

                            if (detail != null)
                            {
                                if (fields.ContainsKey("Name")) fields["Name"] = detail.Name;
                                else fields.Add("Name", detail.Name);

                                if (fields.ContainsKey("Description")) fields["Description"] = detail.Description;
                                else fields.Add("Description", detail.Description);

                                if (fields.ContainsKey("TextPath")) fields["TextPath"] = detail.TextPath;
                                else fields.Add("TextPath", detail.TextPath);
                            }

                            #endregion

                            switch (a)
                            {
                                case "A":   //Add
                                    var add = new AddToIndexModel { CompanyID = companyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.AddToIndex, Type = detail.TypeName };
                                    indexCollectionModel.Adds.Add(add);
                                    break;
                                case "U":   //Update
                                    var update = new UpdateInIndexModel { CompanyID = companyID, Fields = fields, Group = o, ID = oid, RelativeUrl = detail.Url, To = QueueAction.UpdateInIndex, Type = detail.TypeName };
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

                        var total = companyConnection.Query<int>("select count(1) from [queue].[Task] where MachineAssigned is null and NumberOfRetries < 2").Single();
                        var queueItems = companyConnection.Query<QueueTask>($"select top {numberOfQueueItems} * from [queue].[Task] where MachineAssigned is null and NumberOfRetries < 2 order by [Priority] asc, [Date] asc").ToList();

                        if (total > numberOfQueueItems)
                        {
                            shouldRunAgain = true;
                        }

                        Console.WriteLine("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                        queueItems.ForEach(q =>
                        {
                            companyConnection.Execute("update [queue].[Task] set MachineAssigned = @m where ID = @queueID", new { m = System.Environment.MachineName, queueID = q.ID });
                        });

                        queueItems.ForEach(q =>
                        {
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

                                            resolveIndexItem(q.Object, q.ObjectID, "A");
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

                                            resolveIndexItem(q.Object, q.ObjectID, "D");
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

                                            resolveIndexItem(q.Object, q.ObjectID, "U");
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
                                                        tags.Add("ownerUrl", string.Format("https://{0}.data3sixty.com/{1}", domainPrefix, comment.OwnerUrl));
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
                                                var execution = companyConnection.Query<d360.core.entities.FusionExecution>(@"select * from fusion.Execution where ID = @id", new { id = q.ObjectID }, null, true, 900).FirstOrDefault();

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
                                                        tags.Add("fusionUrl", string.Format("https://{0}.data3sixty.com/#/fusion/{1}/{2}", domainPrefix, fusionInfo.FusionTypeID, fusionInfo.FusionID));
                                                        tags.Add("executionUrl", string.Format("https://{0}.data3sixty.com/#/fusion/{1}/{2}/executions/{3}", domainPrefix, fusionInfo.FusionTypeID, fusionInfo.FusionID, execution.ID));
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
                                        resolveIndexItem(q.Object, q.ObjectID, q.Custom);
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
                            }
                            catch (Exception ex)
                            {
                                if (q.NumberOfRetries >= 2)
                                    companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                else
                                    companyConnection.Execute(@"update [queue].[Task] set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
                            }
                        });

                        companyConnection.Close();
                        companyConnection.Dispose();

                        #region Now deal with INDEXING

                        try
                        {
                            var search = new extensions.search.AzureSearchSource();

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
                            Console.WriteLine(msg);
                        }

                        #endregion
                    });
                } //end while shouldrunagain
            }
            catch (Exception ex)
            {
                var msg = ex.GetFullExceptionData();
                Console.WriteLine(msg);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
