using System.Activities;
using d360.utils.company;
using Dapper;
using d360.core.entities;
using System.Collections.Generic;

namespace d360.workflow
{

    public sealed class GetResourceByID : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<string> AssignToResourceObject { get; set; }
        public InArgument<int> AssignToResourceObjectID { get; set; }
        public OutArgument<List<Resource>> Users { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var assignToResourceObject = context.GetValue(this.AssignToResourceObject);
            var assignToResourceObjectID = context.GetValue(this.AssignToResourceObjectID);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                var resources = connection.Query<Resource>(
                    @"select ResourceID as ID, FirstName, LastName, Email, DateLastLoggedIn, 1 as ResourceTypeID, Status from reporting.Global_Resource where ResourceID = @id",
                    new { id = assignToResourceObjectID }
                ).AsList();
                connection.Close();

                context.SetValue<List<Resource>>(this.Users, resources);
            }
        }
    }
}
