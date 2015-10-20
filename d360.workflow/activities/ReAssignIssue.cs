using System.Activities;
using d360.utils.company;
using Dapper;

namespace d360.workflow
{

    public sealed class ReAssignIssue : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> IssueID { get; set; }
        public InArgument<string> Comment { get; set; }
        public InArgument<int> ResourceID { get; set; }
        public InArgument<string> AssignToResourceObject { get; set; }
        public InArgument<int> AssignToResourceObjectID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var issueID = context.GetValue(this.IssueID);
            var comment = context.GetValue(this.Comment);
            var resourceID = context.GetValue(this.ResourceID);
            var assignToResourceObject = context.GetValue(this.AssignToResourceObject);
            var assignToResourceObjectID = context.GetValue(this.AssignToResourceObjectID);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                connection.Execute(
                    @"insert into IssueAssignment (IssueID, ResponsibleObject, ResponsibleObjectID, AssignedOn, AssignedBy, IsActive) values (@I, @RO, @RI, getutcdate(), @AB, 1)",
                    new { I = issueID, RO = assignToResourceObject, RI = assignToResourceObjectID, AB = resourceID }
                );
                connection.Close();
            }
        }
    }
}
