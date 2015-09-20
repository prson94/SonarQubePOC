using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Diagnostics;
using Dapper;
using d360.core.entities;
using SpreadsheetLight;

namespace d360.jobs.ProcessDatabaseQueues
{
    public class Functions: FunctionsBase
    {
        public static List<Exception> ProcessDatabaseQueue()
        {
            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs().Where(i => i == 8).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    //while (companyConnection.ExecuteScalar<int>(@"select count(1) from utility.Queue where MachineAssigned is null") > 0)
                    //{
                    var queueItems = companyConnection.Query<QueueItem>(@"select top 150 ID, ObjectType, ObjectID, Action, Date, null as Data from utility.Queue where MachineAssigned is null order by Date asc").ToList();
                        Trace.TraceInformation("Found {0} queue items for company {1}.  Starting to process them.", queueItems.Count, companyID);

                        queueItems.AsParallel().WithDegreeOfParallelism(1).ForAll(q =>
                        {
                            var innerCompanyConnection = GetCompanyConnection(companyID);
                            innerCompanyConnection.Open();

                            innerCompanyConnection.Execute("update utility.Queue set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID }, null, 500);      // 5 minute timeout

                            Trace.TraceInformation("Executing task for {0} for company {1} with info ({2}, {3}).", q.Action, companyID, q.ObjectType, q.ObjectID);

                            switch (q.Action)
                            {
                                case "BulkLoadAction":
                                    #region
                                    try
                                    {
                                        var load = innerCompanyConnection.Query<Load>("select * from Load where ID = @id", new { id = q.ObjectID }).SingleOrDefault();

                                        var fields = innerCompanyConnection.Query<LoadTypeField>(
                                            "select * from LoadTypeField where LoadTypeID = @id order by SortOrder",
                                            new { id = load.LoadTypeID }
                                        ).ToList();

                                        var memoryStream = new MemoryStream(load.File);
                                        var xls = new SLDocument(memoryStream);

                                        var stats = xls.GetWorksheetStatistics();
                                        var rowIndex = stats.StartRowIndex + 1;
                                        while (rowIndex <= stats.EndRowIndex)
                                        {
                                            var loadItemID = innerCompanyConnection.ExecuteScalar<int>("insert into LoadItem (LoadID, RowIndex) values (@l, @r); select SCOPE_IDENTITY()", new { l = load.ID, r = rowIndex });
                                            var columnIndex = stats.StartColumnIndex;

                                            while (columnIndex <= stats.EndColumnIndex)
                                            {
                                                var field = fields[columnIndex - 1];
                                                if (field != null)
                                                {
                                                    innerCompanyConnection.Execute("insert into LoadItemField (LoadItemID, LoadTypeFieldID, Value) values (@l, @f, @v)", new { l = loadItemID, f = field.ID, v = xls.GetCellValueAsString(rowIndex, columnIndex) });
                                                }
                                                columnIndex++;
                                            }

                                            rowIndex++;
                                        }

                                        bool bulkLoadWriteStatus = true;
                                        var bulkLoadTask = innerCompanyConnection.ExecuteAsync("exec ProcessBulkLoad @LoadID", new { LoadID = q.ObjectID }, null, 1800);    // 30 minute timeout.
                                        bulkLoadTask.ContinueWith(t =>
                                        {
                                            if (t.IsCompleted)
                                                Console.WriteLine("Bulk load procedure completed for Load ID {0}", q.ObjectID);
                                            if(t.IsFaulted)
                                                Console.WriteLine("Bulk load procedure failed for Load ID {0}", q.ObjectID);

                                            bulkLoadWriteStatus = false;
                                        });

                                        while (bulkLoadWriteStatus)
                                        {
                                            Console.WriteLine("Bulk load procedure executing...");
                                            System.Threading.Thread.Sleep(15000);
                                        }
                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("BulkLoadAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }

                                    #endregion
                                    break;
                                case "CacheObjectAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("CacheObjectAction. Start processing queue item {0} for company {1}.", q.ID, companyID);
                                        innerCompanyConnection.Execute("EXEC cache.SynchronizeObjectDetails @type, @id", new { type = q.ObjectType, id = q.ObjectID }, null, 7200);
                                        Trace.TraceInformation("CacheObjectAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("CacheObjectAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "CacheResponsibilityAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("CacheResponsibilityAction. Start processing queue item {0} for company {1}.", q.ID, companyID);
                                        innerCompanyConnection.Execute("EXEC cache.SynchronizeResponsibilities @ID, 0", new { ID = q.ObjectID }, null, 7200);
                                        Trace.TraceInformation("CacheResponsibilityAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("CacheResponsibilityAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "UnCacheResponsibilityAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("UnCacheResponsibilityAction. Start processing queue item {0} for company {1}.", q.ID, companyID);
                                        //innerCompanyConnection.Execute("EXEC cache.SynchronizeResponsibilities @ID, 1", new { ID = q.ObjectID }, null, 7200);
                                        innerCompanyConnection.Execute("EXEC cache.SynchronizeResponsibilities", new { }, null, 7200);
                                        Trace.TraceInformation("UnCacheResponsibilityAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("UnCacheResponsibilityAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "CacheStyleObjectAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("CacheStyleObjectAction. Start processing queue item {0} for company {1}.", q.ID, companyID);
                                        innerCompanyConnection.Execute(
@"
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
        inner join ObjectStyle S on S.ObjectType = @type and S.ObjectID = @id and T.[Object] = S.ObjectType and T.ObjectID = S.ObjectID;",
                                        new { type = q.ObjectType, id = q.ObjectID }, null, 7200);
                                        Trace.TraceInformation("CacheStyleObjectAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("CacheStyleObjectAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "CalculateStatisticsAction":
                                    #region
                                    try
                                    {
                                        bool calculateStatisticsWriteStatus = true;
                                        Task<int> calculateStatisticsTask = null;

                                        if (q.ObjectType == "StatisticType")
                                        {
                                            calculateStatisticsTask = innerCompanyConnection.ExecuteAsync("exec utility.CalculateStatistics @TargetStatisticTypeID", new { TargetStatisticTypeID = q.ObjectID }, null, 600);    // 10 minute timeout.
                                        }
                                        else
                                        {
                                            calculateStatisticsTask = innerCompanyConnection.ExecuteAsync("exec utility.CalculateStatistics", null, null, 600);    // 10 minute timeout.
                                        }

                                        calculateStatisticsTask.ContinueWith(t =>
                                        {
                                            if (t.IsCompleted)
                                                Console.WriteLine("Calculate statistics procedure completed");
                                            if (t.IsFaulted)
                                                Console.WriteLine("Calculate statistics procedure failed");

                                            calculateStatisticsWriteStatus = false;
                                        });

                                        while (calculateStatisticsWriteStatus)
                                        {
                                            Console.WriteLine("Calculate statistics procedure executing...");
                                            System.Threading.Thread.Sleep(15000);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        mex.Add(ex);
                                    }

                                    #endregion
                                    break;
                                case "CalculateStatisticsForObjectAction":
                                    #region
                                    try
                                    {
                                        innerCompanyConnection.Execute("exec utility.CalculateStatistics @Type, @ID", new { Type = q.ObjectType, ID = q.ObjectID }, null, 120);    // 2 minute timeout.
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("CalculateStatisticsForObjectAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "CommentNotificationAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("CommentNotificationAction. Start processing queue item {0} for company {1}.", q.ID, companyID);
                                        var domainPrefix = domainPrefixes.First(i => i.Key == companyID).Value;

                                        var comment = innerCompanyConnection.Query<CommentInfo>(
@"select	C.ID,
		C.Body,
		C.DateCreated,
		R.FirstName + ' ' + R.LastName as Author,
		C.ParentID,
		P.Body as ParentBody,
		P.DateCreated as ParentDateCreated,
		PR.FirstName + ' ' + PR.LastName as ParentAuthor,
		D.Name as OwnerName,
		D.Url as OwnerUrl,
		D.ObjectTypeName as OwnerTypeName,
		case when C.ParentID is null then 'comment' else 'reply' end as OriginationType
from	Comment C
		inner join reporting.Global_Resource R on R.ResourceID = C.CreatingResourceID and C.ID = @CommentID
		inner join cache.ObjectDetails D on D.[Object] = C.OwnerObjectType and D.ObjectID = C.OwnerObjectID
		left join Comment P on P.ID = C.ParentID
		left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID", 
                                            new { CommentID = q.ObjectID }, null, true, 900
                                        ).FirstOrDefault();

                                        if (comment != null)
                                        {
                                            
                                            var resourcesToNotify = innerCompanyConnection.Query<CommentNotificationUser>(
@"select	F.ResourceID,
		R.FirstName + ' ' + R.LastName as Name,
		R.Email
from	CommentRelation CR
		inner join Follow F on F.ObjectType = CR.ObjectType and F.ObjectID = CR.ObjectID  and CR.CommentID = @CommentID
		inner join reporting.Global_Resource R on R.ResourceID = F.ResourceID and R.Email not like '%?subject=%'
union
select	coalesce(RG.ResourceID, R.ResponsibleObjectID) as ResourceID,
		RE.FirstName + ' ' + RE.LastName as Name,
		RE.Email
from	CommentRelation CR
		inner join ResponsibilityDetail R on R.ObjectType = CR.ObjectType and R.ObjectID = CR.ObjectID and CR.CommentID = @CommentID
		left join ResourceGroup RG on R.ResponsibleObjectType = 'Group' and RG.GroupID = R.ResponsibleObjectID
		inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'", 
                                                new { CommentID = q.ObjectID }, null, true, 900
                                            ).ToList();

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
                                        
                                        Trace.TraceInformation("CommentNotificationAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("CommentNotificationAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                                case "ProcessFusionForObjectAction":
                                    #region
                                    try
                                    {
                                        Trace.TraceInformation("ProcessFusionForObjectAction. Start processing queue item {0} for company {1}.", q.ID, companyID);

                                        bool processFusionWriteStatus = true;
                                        var processFusionTask = innerCompanyConnection.ExecuteAsync("exec fusion.ProcessFusionInQueue @queueID", new { queueID = q.ID }, null, 7200);    // 120 minute timeout.
                                        processFusionTask.ContinueWith(t =>
                                        {
                                            if (t.IsCompleted)
                                                Console.WriteLine("Process fusion procedure completed for queue ID {0}, company {1}", q.ObjectID, companyID);
                                            if (t.IsFaulted)
                                                Console.WriteLine("Process fusion procedure failed for queue ID {0}, company {1}", q.ObjectID, companyID);

                                            processFusionWriteStatus = false;
                                        });

                                        while (processFusionWriteStatus)
                                        {
                                            Console.WriteLine("Process fusion procedure executing...");
                                            System.Threading.Thread.Sleep(15000);
                                        }                                        
                                        
                                        Trace.TraceInformation("ProcessFusionForObjectAction. Finished processing queue item {0} for company {1}.", q.ID, companyID);
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.TraceError("ProcessFusionForObjectAction. Error while processing queue item {0} for company {1}. Exception was: ", q.ID, companyID, ex.Message + " " + ((ex.InnerException != null) ? ex.InnerException.Message : ""));
                                        mex.Add(ex);
                                    }
                                    #endregion
                                    break;
                            }

                            if (mex.Count == 0)
                            {
                                innerCompanyConnection.Execute("delete utility.Queue where ID = @queueID", new { queueID = q.ID }, null, 500);      // 5 minute timeout
                                innerCompanyConnection.Close();
                                innerCompanyConnection.Dispose();
                            }
                        });                    
                    //}

                    companyConnection.Close();
                    companyConnection.Dispose();
                });
            }
            catch (Exception ex)
            {
                var msg = ex.Message + ((ex.InnerException != null) ? "  " + ex.InnerException.Message : "");
                Trace.TraceError(msg);
            }

            return mex;
        }
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
}
