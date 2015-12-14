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
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                bool shouldRunAgain = true;

                while (shouldRunAgain)
                {
                    shouldRunAgain = false;

                    //var companies = new List<int>() { 4 };
                    var companies = GetActiveCompanyIDs();

                    var domainPrefixes = GetCompanyDomainPrefixes();

                    #region Must keep this segment here b/c this webjob can execute on multiple machines.  We do not want two or more machines trying to grab hold of the same queue item.
                    var rand = new Random();
                    int sleepSeconds = rand.Next(1, 10);
                    Thread.Sleep(sleepSeconds * 1000);
                    #endregion

                    companies.AsParallel().WithDegreeOfParallelism(3).ForAll(companyID =>
                    {
                        var companyConnection = GetCompanyConnection(companyID);
                        companyConnection.Open();

                        var total = companyConnection.Query<int>("select count(1) from [queue].[Task] where MachineAssigned is null and NumberOfRetries < 3").Single();
                        var queueItems = companyConnection.Query<QueueTask>(@"select top 25 * from [queue].[Task] where MachineAssigned is null and NumberOfRetries < 3 order by [Date] asc").ToList();

                        if (total > 25)
                        {
                            shouldRunAgain = true;
                        }

                        Console.WriteLine("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                        queueItems.ForEach(q =>
                        {
                            companyConnection.Execute("update [queue].[Task] set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                        });

                        queueItems.ForEach(q =>
                        {
                            try
                            {
                                bool processFusionWriteStatus = true;
                                Task<int> task = null;
                                bool hasNotAsyncTaskError = false;
                                string asyncTaskError = "";

                                switch (q.Action)
                                {
                                    case "Analytic":
                                        #region
                                        task = companyConnection.ExecuteAsync("exec utility.CalculateStatistics @Type, @ID", new { Type = q.Object, ID = q.ObjectID }, null, 120);    // 2 minute timeout.
                                        break;
                                    #endregion
                                    case "Add":
                                        #region
                                        if (!string.IsNullOrEmpty(q.Custom))
                                        {
                                            var customXml = XElement.Parse(q.Custom);
                                            task = companyConnection.ExecuteAsync(
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
                                        }
                                        break;
                                    #endregion
                                    case "Delete":
                                        #region
                                        if (!string.IsNullOrEmpty(q.Custom))
                                        {
                                            var customXml = XElement.Parse(q.Custom);
                                            task = companyConnection.ExecuteAsync(
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
                                        }
                                        break;
                                    #endregion
                                    case "Update":
                                        #region
                                        if (!string.IsNullOrEmpty(q.Custom))
                                        {
                                            var customXml = XElement.Parse(q.Custom);
                                            task = companyConnection.ExecuteAsync(
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
                                        }
                                        break;
                                    #endregion
                                    case "FollowChildren":
                                        #region
                                        try
                                        {
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

                                        }
                                        catch (Exception ex)
                                        {
                                            hasNotAsyncTaskError = true;
                                            asyncTaskError = ex.GetFullExceptionData();
                                        }
                                        break;
                                    #endregion
                                    case "FusionCache":
                                        #region
                                        task = companyConnection.ExecuteAsync("exec fusion.ProcessFusionCacheInQueue @FusionID", new { FusionID = q.ObjectID }, null, 10800);    // 180 minute timeout.
                                        break;
                                    #endregion
                                    case "Notify":
                                        #region
                                        try
                                        {
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
                                        }
                                        catch (Exception ex)
                                        {
                                            hasNotAsyncTaskError = true;
                                            asyncTaskError = ex.GetFullExceptionData();
                                        }
                                        break;
                                    #endregion
                                    case "ObjectCache":
                                        #region
                                        task = companyConnection.ExecuteAsync("exec cache.SynchronizeObjectDetails @type, @id", new { type = q.Object, id = q.ObjectID }, null, 180);
                                        break;
                                    #endregion
                                    case "ObjectIndex":
                                        #region
                                        ObjectDetail detail = null;
                                        Dictionary<string, string> fields = null;

                                        #region Load Info for Object

                                        detail = companyConnection.Query<ObjectDetail>("SELECT * FROM utility.ObjectDetail(@t, @i)", new { t = q.Object, i = q.ObjectID }).SingleOrDefault();
                                        fields = companyConnection.Query<FieldWithRelation>(
                                            "SELECT * from FieldWithRelation where ObjectType = @t and ObjectID = @i order by SortOrder",
                                            new { t = q.Object, i = q.ObjectID }
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

                                        try
                                        {
                                            var search = new extensions.search.AzureSearchSource();
                                            switch (q.Custom)
                                            {
                                                case "A":   //Add
                                                    var add = new AddToIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = detail.Url, To = QueueAction.AddToIndex, Type = detail.TypeName };
                                                    search.AddToIndex(add);
                                                    break;
                                                case "U":   //Update
                                                    var update = new UpdateInIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = detail.Url, To = QueueAction.UpdateInIndex, Type = detail.TypeName };
                                                    search.UpdateInIndex(update);
                                                    break;
                                                case "D":   //Delete
                                                    var delete = new RemoveFromIndexModel { CompanyID = companyID, Fields = fields, Group = q.Object, ID = q.ObjectID, RelativeUrl = "#", To = QueueAction.RemoveFromIndex }; //, Type = detail.TypeName
                                                    search.RemoveFromIndex(delete);
                                                    break;
                                            }
                                            search = null;
                                        }
                                        catch (Exception ex)
                                        {
                                            hasNotAsyncTaskError = true;
                                            asyncTaskError = ex.GetFullExceptionData();
                                        }
                                        break;
                                    #endregion
                                    case "ObjectStyleCache":
                                        #region
                                        task = companyConnection.ExecuteAsync(Sql.StyleCache, new { type = q.Object, id = q.ObjectID }, null, 7200);
                                        break;
                                    #endregion
                                    case "ObjectVersion":
                                        #region
                                        if (!string.IsNullOrEmpty(q.Custom))
                                        {
                                            var customXml = XElement.Parse(q.Custom);
                                            task = companyConnection.ExecuteAsync(
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
                                        //case "Recache":
                                        //    #region
                                        //    break;
                                        //    #endregion
                                }

                                if (task != null)
                                {
                                    task.ContinueWith(t =>
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
                                                if (q.NumberOfRetries >= 3)
                                                {
                                                    companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                                }
                                                else
                                                {
                                                    companyConnection.Execute(@"update [queue].[Task] set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = exceptionData }, null, 500);
                                                }
                                                
                                            }
                                            else
                                            {
                                                companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                            }
                                        }

                                        processFusionWriteStatus = false;
                                    });

                                    while (processFusionWriteStatus && (task.Exception == null))
                                    {
                                        Console.WriteLine("Executing company {0}, queue {1}...", companyID, q.ID);
                                        Thread.Sleep(2500);
                                    }
                                }
                                else
                                {
                                    if (hasNotAsyncTaskError)
                                    {
                                        companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                    }
                                    else
                                    {
                                        Console.WriteLine(asyncTaskError);
                                        if (q.NumberOfRetries >= 2)
                                        {
                                            companyConnection.Execute("delete [queue].[Task] where ID = @queueID", new { queueID = q.ID }, null, 500);
                                        }
                                        else
                                        {
                                            companyConnection.Execute(@"update [queue].[Task] set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = asyncTaskError }, null, 500);
                                        }
                                    }
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
