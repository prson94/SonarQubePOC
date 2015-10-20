using System;
using System.Activities;
using d360.core;
using d360.utils.company;
using Dapper;
using System.Activities.Tracking;

namespace d360.workflow
{
    public sealed class MarkPreviousUserAssignmentsAsCompleted : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var companyID = context.GetValue(this.CompanyID);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();

                    cnn.Execute(
                        @"update WorkflowResource set IsComplete = 1 where WorkflowID = @WorkflowID",
                        new { WorkflowID = context.WorkflowInstanceId }
                    );

                    cnn.Close();
                }
            }
            catch (Exception ex)
            {
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "MarkPreviousUserAssignmentsAsCompleted", System.Diagnostics.TraceLevel.Error)
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
