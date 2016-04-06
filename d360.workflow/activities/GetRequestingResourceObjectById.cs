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
using d360.workflow.models;

namespace d360.workflow
{
    public sealed class GetRequestingResourceObjectById : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> ResourceID { get; set; }
        public OutArgument<Resource> RequestingResource { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);

            var requestResourceID = context.GetValue(this.ResourceID);

            var connection = CompanyConnectionUtils.GetCompanyConnection(companyID);
            connection.Open();

            var resource = connection.Query<Resource>(
@"select    ResourceID as ID,
		    FirstName,
		    LastName,
		    Email,
		    Email as Username,
		    DateLastLoggedIn,
		    1 as ResourceTypeID,
		    Status 
from        reporting.Global_Resource 
where   ResourceID = @r", new { r = requestResourceID }).SingleOrDefault();

            connection.Close();

            context.SetValue<Resource>(this.RequestingResource, resource);
            connection.Dispose();
        }
    }
}
