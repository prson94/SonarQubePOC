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
    public sealed class MarkUserAsCompletedOnActivity : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<ActivityType> Activity { get; set; }
        public InArgument<int> ResourceID { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            var activity = context.GetValue(this.Activity);
            var companyID = context.GetValue(this.CompanyID);
            var resourceID = context.GetValue(this.ResourceID);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();

                    cnn.Execute(
                        @"update WorkflowResource set IsComplete = 1 where WorkflowID = @WorkflowID and Activity = @Activity and ResourceID = @ResourceID",
                        new { WorkflowID = context.WorkflowInstanceId, Activity = (int)activity, ResourceID = resourceID }
                    );

                    cnn.Close();
                }

                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "User has completed activity", System.Diagnostics.TraceLevel.Info)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"ResourceID", resourceID},
                            {"Activity", activity}
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
