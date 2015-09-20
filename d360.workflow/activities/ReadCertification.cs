using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.workflow.models;
using System.Activities.Tracking;

namespace d360.workflow
{

    public sealed class ReadCertification : NativeActivity
    {
        public OutArgument<int> ResourceID { get; set; }
        public InArgument<int> CompanyID { get; set; }

        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("CertificationFromOwner", new BookmarkCallback(OnReadComplete));//, BookmarkOptions.MultipleResume);
        }

        void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            var companyID = CompanyID.Get(context);
            var approval = state as CertificationApproval;
            context.SetValue<int>(this.ResourceID, approval.ResourceID);

            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Read Certification", System.Diagnostics.TraceLevel.Info)
            {
                Data = 
                        {
                            {"CompanyID", companyID},
                            {"ResourceID", approval.ResourceID}
                        }
            };
            context.Track(trackingRecord);
        }
    }
}
