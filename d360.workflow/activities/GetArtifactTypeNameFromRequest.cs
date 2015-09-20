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

namespace d360.workflow
{

    public sealed class GetArtifactTypeNameFromRequest : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<NewArtifactRequest> ArtifactRequest { get; set; }
        public OutArgument<string> ArtifactTypeName { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var request = context.GetValue(this.ArtifactRequest);
            var artifactTypeName = "";

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                artifactTypeName = connection.Query<string>(@"select Name from ArtifactType where ID = @id", new { id = request.ArtifactTypeID }).SingleOrDefault();
                connection.Close();

                context.SetValue<string>(this.ArtifactTypeName, artifactTypeName);
            }
        }
    }
}
