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
using d360.workflow;
using System.Activities.Tracking;
using d360.core.enums;
using System.Xml.Linq;

namespace d360.workflow
{
    public sealed class AssociateUsersWithWorkflow : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<ActivityType> Activity { get; set; }
        public InArgument<List<Resource>> Users { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            var activity = context.GetValue(this.Activity);
            var companyID = context.GetValue(this.CompanyID);
            var users = context.GetValue(this.Users);
            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();

                    foreach (var user in users)
                    {
                        cnn.Execute(
                            @"insert into WorkflowResource values (@WorkflowID, @Activity, @ResourceID, 0)",
                            new { WorkflowID = context.WorkflowInstanceId, Activity = (int)activity, ResourceID = user.ID }
                        );
                    }

                    cnn.Close();
                }

                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Associating Users With Workflow", System.Diagnostics.TraceLevel.Info)
                {
                    Data = 
                        {
                            {"CompanyID", companyID}
                        }
                };
                context.Track(trackingRecord);
            }
            catch (Exception ex)
            {
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Associating Users With Workflow", System.Diagnostics.TraceLevel.Error)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"ErrorMessage", ex.GetFullExceptionData()}
                        }
                };
                context.Track(trackingRecord);
            }  
        }
    }
}
