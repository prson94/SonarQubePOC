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
    public sealed class GetWorkflowOwners : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public OutArgument<List<Resource>> Owners { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);

            int ownerCount = 0;

            using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                cnn.Open();

                var owners = cnn.Query<Resource>(@"exec utility.GetOwnersForWorkflow @w", new { w = context.WorkflowInstanceId }).ToList();

                cnn.Close();

                ownerCount = owners.Count;

                context.SetValue<List<Resource>>(this.Owners, owners);
                cnn.Dispose();            
            }


            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Get Artifact Owners", System.Diagnostics.TraceLevel.Info)
            {
                Data = 
                        {
                            {"CompanyID", companyID},
                            {"OwnerCount", ownerCount}
                        }
            };
            context.Track(trackingRecord);
        }
    }
}
