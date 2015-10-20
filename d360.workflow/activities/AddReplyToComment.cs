using System.Activities;
using d360.utils.company;
using Dapper;

namespace d360.workflow
{

    public sealed class AddReplyToComment : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> CommentID { get; set; }
        public InArgument<int> CommentTypeID { get; set; }
        public InArgument<string> Comment { get; set; }
        public InArgument<int> ResourceID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var commentID = context.GetValue(this.CommentID);
            var commentTypeID = context.GetValue(this.CommentTypeID);
            var comment = context.GetValue(this.Comment);
            var resourceID = context.GetValue(this.ResourceID);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                connection.Execute(
                    @"insert into Comment (ParentID, CommentTypeID, Body, DateCreated, CreatingResourceID, OwnerObjectType, OwnerObjectID) values (@P, @T, @B, getutcdate(), @OI, 'Resource', @OI)",
                    new { P = commentID, T = commentTypeID, B = comment, OI = resourceID }
                );
                connection.Close();
            }
        }
    }
}
