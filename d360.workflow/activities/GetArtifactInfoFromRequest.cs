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

    public sealed class GetArtifactInfoFromRequest : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<CertifyArtifactRequest> CertifyRequest { get; set; }
        public OutArgument<string> ArtifactName { get; set; }
        public OutArgument<string> ArtifactTypeName { get; set; }
        public OutArgument<string> ArtifactUrl { get; set; }
        public OutArgument<int> ArtifactTypeID { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var request = context.GetValue(this.CertifyRequest);

            using (var connection = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                connection.Open();
                var o = connection.Query<dynamic>(
@"select A.Name as ArtifactName, 
        T.Name as ArtifactTypeName, 
        A.ArtifactTypeID, 
        dbo.GenerateObjectUrl('Artifact', T.ID, A.ID) as ArtifactUrl 
from    Artifact A 
        inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ID = @id", new { id = request.ArtifactID }).SingleOrDefault();
                connection.Close();

                if (o != null)
                {
                    context.SetValue<string>(this.ArtifactName, o.ArtifactName);
                    context.SetValue<int>(this.ArtifactTypeID, o.ArtifactTypeID);
                    context.SetValue<string>(this.ArtifactTypeName, o.ArtifactTypeName);
                    context.SetValue<string>(this.ArtifactUrl, o.ArtifactUrl);
                }
            }
        }
    }
}
