using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.core;
using System.Diagnostics;
using Dapper;

namespace d360.jobs.queue.ProcessNotification
{
    #region Models

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

    class Program: FunctionsBase
    {
        #region SQL Statements

        static string notificationSql = @"select n.* from queue.Notification n
inner join Comment c on 
	n.[Object] = 'Comment' 
	AND ObjectId = c.ID 
	AND  (
			(select count(*) from comment r where r.ParentID = c.ID) > 0
			OR (
				c.ParentID IS NOT NULL
				OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0))
			)
		 )
where n.MachineAssigned IS NULL
union all
select * from queue.Notification where [Object] != 'Comment' and MachineAssigned IS NULL";


        static string commentSql = @"select	C.ID,
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
left join reporting.Global_Resource PR on PR.ResourceID = P.CreatingResourceID
where (select count(*) from comment where parentID = @CommentID) > 0 OR C.DateCreated < (getdate() - (5 / 24.0 / 60.0)) ";

        static string resourcesSql = @"select	F.ResourceID,
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
inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'";

        static string fusionResourcesSql = @"
select	coalesce(RG.ResourceID, R.ResponsibleObjectID) as ResourceID,
RE.FirstName + ' ' + RE.LastName as Name,
RE.Email
from	cache.Responsibilities CR
inner join ResponsibilityDetail R on R.ObjectType = CR.[Object] and R.ObjectID = CR.ObjectID and CR.[Object] = 'Fusion' and CR.ObjectID = @id
left join ResourceGroup RG on R.ResponsibleObjectType = 'Group' and RG.GroupID = R.ResponsibleObjectID
inner join reporting.Global_Resource RE on RE.ResourceID = coalesce(RG.ResourceID, R.ResponsibleObjectID) and RE.Email not like '%?subject=%'";

        #endregion

        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();//.Where(i => i == 9).ToList();
                var domainPrefixes = GetCompanyDomainPrefixes();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var companyConnection = GetCompanyConnection(companyID);
                    companyConnection.Open();

                    var domainPrefix = domainPrefixes.First(i => i.Key == companyID).Value;
                    var queueItems = companyConnection.Query<dynamic>(notificationSql).ToList();

                    queueItems.ForEach(q =>
                    {
                        companyConnection.Execute("update [queue].Notification set MachineAssigned = @m where ID = @queueID", new { m = Environment.MachineName, queueID = q.ID });
                    });

                    queueItems.ForEach(q =>
                    {
                        try
                        {
                            switch ((string)q.Object)
                            { 
                                case "Comment":
                                #region
                                    var comment = companyConnection.Query<CommentInfo>(commentSql, new { CommentID = q.ObjectID }, null, true, 900).FirstOrDefault();

                                    if (comment != null)
                                    {
                                        var resourcesToNotify = companyConnection.Query<CommentNotificationUser>(resourcesSql, new { CommentID = q.ObjectID }, null, true, 900).ToList();

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
                                        var fusionInfo = companyConnection.Query<dynamic>(
@"select F.ID as FusionID, 
F.Name as Fusion, 
FT.ID as FusionTypeID, 
FT.Name as FusionType
from Fusion F 
inner join FusionType FT on FT.ID = F.FusionTypeID and F.ID = @id", new { id = execution.FusionID }).FirstOrDefault();

                                        var resourcesToNotify = companyConnection.Query<dynamic>(fusionResourcesSql, new { id = execution.FusionID }, null, true, 900).ToList();

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

                            companyConnection.Execute("delete [queue].Notification where ID = @queueID", new { queueID = q.ID }, null, 500);
                        }
                        catch (Exception ex)
                        {
                            mex.Add(ex);
                            companyConnection.Execute(@"update [queue].Notification set MachineAssigned = null, HasError = 1, NumberOfRetries = NumberOfRetries + 1, ErrorMessage = @error where ID = @queueID", new { queueID = q.ID, error = ex.GetFullExceptionData() }, null, 500);
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
