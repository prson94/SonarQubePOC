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
    public sealed class CompleteWorkflowInDatabase : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            var companyID = context.GetValue(this.CompanyID);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();
                    cnn.Execute(@"update Workflow set DateCompleted = @date where ID = @id", new { date = DateTime.UtcNow, id = context.WorkflowInstanceId });
                    cnn.Close();
                }

                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Marking Workflow as Complete", System.Diagnostics.TraceLevel.Info)
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
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Marking Workflow as Complete", System.Diagnostics.TraceLevel.Error)
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
