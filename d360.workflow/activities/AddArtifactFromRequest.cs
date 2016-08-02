using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.utils.company;
using d360.core.entities;
using Dapper;
using System.Xml.Linq;
using System.Activities.Tracking;
using d360.workflow.models;
using d360.core;

namespace d360.workflow
{

    public sealed class AddArtifactFromRequest : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<NewArtifactRequest> ArtifactRequest { get; set; }
        public OutArgument<int> ArtifactID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var request = context.GetValue(this.ArtifactRequest);
            int artifactID = 0;

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();

                artifactID = connection.ExecuteScalar<int>(
@"insert into Artifact (Name, Description, ArtifactTypeID, TaxonomyTypeID, Status, ParentID) values (@N, @D, @A, @V, @S, @P); select scope_identity()",
new { N = request.Name, D = request.Description, A = request.ArtifactTypeID, V = request.TaxonomyTypeID, S = constants.ARTIFACT_STATUS_DRAFT, P = request.ParentID }
);

                if (artifactID > 0)
                {
                    if (request.Fields != null)
                    {
                        foreach (var k in request.Fields.Keys)
                        {
                            connection.Execute(
                                @"insert into Field (ObjectType, ObjectID, FieldTypeID, Value) values (@T, @I, @F, @V)",
                                new { T = "Artifact", I = artifactID, F = int.Parse(k.ToString().Replace("FieldType_", "")), V = request.Fields[k].ToString() }
                            );
                        }
                    }
                    context.SetValue<int>(this.ArtifactID, artifactID);
                }

                connection.Close();
            }

            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Add Artifact From Request", System.Diagnostics.TraceLevel.Info)
            {
                Data = 
                        {
                            {"CompanyID", companyID},
                            {"ArtifactID", artifactID}
                        }
            };
            context.Track(trackingRecord);     
        }
    }
}
