using System;
using System.Collections.Generic;
using System.Activities;
using d360.core.entities;
using d360.core;
using d360.utils.company;
using Dapper;
using System.Activities.Tracking;

namespace d360.workflow
{
    public sealed class TagUserOnCommentThread : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> CommentID { get; set; }
        public InArgument<List<Resource>> Users { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            var companyID = context.GetValue(this.CompanyID);
            var commentID = context.GetValue(this.CommentID);
            var users = context.GetValue(this.Users);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();

                    foreach (var user in users)
                    {
                        cnn.Execute(
                            @"merge CommentRelation as T
using   (
        select @CommentID, 'Resource', @ResourceID, @Date
        ) 
as S (CommentID, ObjectType, ObjectID, [Date]) 
on (T.CommentID = S.CommentID and T.ObjectType = S.ObjectType and T.ObjectID = S.ObjectID)
when matched then
 update 
 set
  T.[Date] = S.[Date]
when not matched then
 insert (CommentID, ObjectType, ObjectID, [Date])
 values (S.CommentID, S.ObjectType, S.ObjectID, S.[Date]);",
                            new { CommentID = commentID, ResourceID = user.ID, Date = DateTime.UtcNow }
                        );
                    }

                    cnn.Close();
                }

                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Tag User On Comment Thread", System.Diagnostics.TraceLevel.Info)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"CommentID", commentID}
                        }
                };
                context.Track(trackingRecord);
            }
            catch (Exception ex)
            {
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Tag User On Comment Thread", System.Diagnostics.TraceLevel.Error)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"CommentID", commentID},
                            {"ErrorMessage", ex.GetFullExceptionData()}
                        }
                };
                context.Track(trackingRecord);
            }  
        }
    }
}
