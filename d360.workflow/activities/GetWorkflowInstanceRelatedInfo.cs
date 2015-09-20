using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.core.entities;
using System.Data.SqlClient;
using d360.core;
using d360.utils.company;
using Dapper;
using System.Activities.Tracking;

namespace d360.workflow
{
    public sealed class GetWorkflowInstanceRelatedInfo : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public OutArgument<Guid> WorkflowInstanceID { get; set; }
        public OutArgument<string> CompanyDomainPrefix { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);

            context.SetValue<Guid>(this.WorkflowInstanceID, context.WorkflowInstanceId);

            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                cnn.Open();

                var prefix = cnn.Query<string>(@"select UrlPrefix from CompanyDomainSetting where CompanyID = @c and IsPrimary = 1", new { c = companyID }).FirstOrDefault();

                cnn.Close();

                context.SetValue<string>(this.CompanyDomainPrefix, prefix);
                cnn.Dispose();            
            }
        }
    }
}
