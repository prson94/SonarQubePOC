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
    public sealed class UpdateWorkflowStep : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> Step { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var activityTitle = "Update Workflow Step";
            int companyID = context.GetValue(this.CompanyID);
            int step = context.GetValue(this.Step);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();
                    cnn.Execute(@"update Workflow set Step = @s where ID = @id", new { s = step, id = context.WorkflowInstanceId });
                    cnn.Close();
                }
            }
            catch (Exception ex)
            {
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, activityTitle, System.Diagnostics.TraceLevel.Error)
                {
                    Data = 
                        {
                            {"Message", ex.GetFullExceptionData()},
                            {"Step", step}
                        }
                };
                context.Track(trackingRecord);
            }
        }
    }
}
