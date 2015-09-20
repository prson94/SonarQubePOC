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
    public sealed class UpdateArtifactStatus : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> ArtifactID { get; set; }
        public InArgument<string> Status { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var activityTitle = "Update Artifact Status";
            int companyID = context.GetValue(this.CompanyID);
            int artifactID = context.GetValue(this.ArtifactID);
            string status = context.GetValue(this.Status);

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();
                    cnn.Execute(@"update Artifact set Status = @s where ID = @id", new { s = status, id = artifactID });
                    cnn.Close();
                }
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, activityTitle, System.Diagnostics.TraceLevel.Info)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"ArtifactID", artifactID},
                            {"Status", status}
                        }
                };
                context.Track(trackingRecord);
            }
            catch (Exception ex)
            {
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, activityTitle, System.Diagnostics.TraceLevel.Error)
                {
                    Data = 
                        {
                            {"Message", ex.GetFullExceptionData()},
                            {"ArtifactID", artifactID},
                            {"Status", status}
                        }
                };
                context.Track(trackingRecord);
            }
        }
    }
}
