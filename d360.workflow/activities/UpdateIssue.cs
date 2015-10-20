using System.Activities;
using d360.utils.company;
using Dapper;

namespace d360.workflow
{

    public sealed class UpdateIssue : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> IssueID { get; set; }
        public InArgument<int> ResourceID { get; set; }
        public InArgument<string> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var issueID = context.GetValue(this.IssueID);
            var resourceID = context.GetValue(this.ResourceID);
            var status = context.GetValue(this.Status);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                connection.Execute(
                    @"update Issue set Status = @S, UpdatedOn = getutcdate(), UpdatedBy = @R where ID = @ID",
                    new { S = status, R = resourceID, ID = issueID}
                );
                connection.Close();
            }
        }
    }
}
