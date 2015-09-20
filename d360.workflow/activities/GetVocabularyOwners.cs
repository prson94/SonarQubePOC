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
    public sealed class GetVocabularyOwners : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> VocabularyID { get; set; }
        public InArgument<string> ResponsibilityType { get; set; }
        public OutArgument<List<Resource>> Owners { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);

            string responsibilityType = context.GetValue(this.ResponsibilityType);
            int vocabularyID = context.GetValue(this.VocabularyID);
            int ownerCount = 0;

            using (var cnn = CompanyConnectionUtils.GetCompanyConnection(companyID))
            {
                cnn.Open();

                var owners = cnn.Query<Resource>(
    @"select    R.ResourceID as ID,
		    R.FirstName,
		    R.LastName,
		    R.Email,
		    R.Email as Username,
		    R.DateLastLoggedIn,
		    1 as ResourceTypeID,
		    R.Status 
from        ResponsibilityDetail RD 
            inner join reporting.Global_Resource R on RD.ObjectType = @type and RD.ObjectID = @id and RD.[Role] = @role and
(
    (RD.ResponsibleObjectType = 'Group' and R.ResourceID = RD.PrimaryOwnerResourceID) or 
    (RD.ResponsibleObjectType = 'Resource' and R.ResourceID = RD.ResponsibleObjectID)
)
			inner join WorkflowTypeRelationResponsibilityType WTR on WTR.ObjectType = @type and WTR.ObjectID = @id and WTR.WorkflowType = 1 and WTR.ResponsibilityTypeID = RD.ResponsibilityTypeID", 
                new { type = "Vocabulary", id = vocabularyID, role = responsibilityType }).ToList();

                cnn.Close();

                ownerCount = owners.Count;

                context.SetValue<List<Resource>>(this.Owners, owners);
                cnn.Dispose();            
            }


            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Get Vocabulary Owners", System.Diagnostics.TraceLevel.Info)
            {
                Data = 
                        {
                            {"CompanyID", companyID},
                            {"VocabularyID", vocabularyID},
                            {"OwnerCount", ownerCount}
                        }
            };
            context.Track(trackingRecord);  
        }
    }
}
