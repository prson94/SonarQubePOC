using System.Linq;
using System.Activities;
using d360.utils.company;
using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;

namespace d360.workflow
{

    public sealed class GetCommentInfoFromRequest : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> CommentID { get; set; }

        public OutArgument<string> CommentBody { get; set; }
        public OutArgument<int> CommentCreatorResourceID { get; set; }
        public OutArgument<DateTime> DateCreated { get; set; }
        public OutArgument<string> CommentCreatorResourceName { get; set; }
        public OutArgument<CommentDetailTag[]> Tags { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var commentID = context.GetValue(this.CommentID);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                var o = connection.Query<CommentDetail>(@"exec GetCommentDetailByID @id", new { id = commentID }).Where(i => !i.ParentID.HasValue).SingleOrDefault();
                connection.Close();

                if (o != null)
                {
                    context.SetValue<string>(this.CommentBody, o.Body);
                    context.SetValue<int>(this.CommentCreatorResourceID, o.CreatingResourceID);
                    context.SetValue<DateTime>(this.DateCreated, o.DateCreated);
                    context.SetValue<string>(this.CommentCreatorResourceName, o.ResourceName);
                    o.ParseTagXml();
                    o.ParseVoteXml();
                    context.SetValue<CommentDetailTag[]>(this.Tags, o.Tags.ToArray());
                }
            }
        }
    }
}
