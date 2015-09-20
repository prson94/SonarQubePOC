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
    public sealed class MarkArtifactAsCertified : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> ArtifactID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            var activityTitle = "Mark Artifact as Certified";
            int companyID = context.GetValue(this.CompanyID);
            int artifactID = context.GetValue(this.ArtifactID);
            var date = DateTime.UtcNow;

            try
            {
                using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
                {
                    cnn.Open();
                    cnn.Execute(@"update Artifact set DateLastCertified = @date, Status = @s where ID = @id", new { date = date, s = constants.ARTIFACT_STATUS_CERTIFIED, id = artifactID });
                    cnn.Close();
                }
                var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, activityTitle, System.Diagnostics.TraceLevel.Info)
                {
                    Data = 
                        {
                            {"CompanyID", companyID},
                            {"ArtifactID", artifactID},
                            {"Date", date}
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
                            {"Date", date}
                        }
                };
                context.Track(trackingRecord);
            }
        }
    }
}
