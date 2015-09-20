using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.workflow.models;
using System.Activities.Tracking;

namespace d360.workflow
{

    public sealed class ReadApproval : NativeActivity<int>
    {
        //public OutArgument<bool> Result { get; set; }

        public OutArgument<int> ResourceID { get; set; }
        public OutArgument<string> Notes { get; set; }
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> BusinessOwnersCount { get; set; }
        
        // NativeActivity derived activities that do asynchronous operations by calling 
        // one of the CreateBookmark overloads defined on System.Activities.NativeActivityContext 
        // must override the CanInduceIdle property and return true.
        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void Execute(NativeActivityContext context)
        {
            context.CreateBookmark("ApprovalFromOwner", new BookmarkCallback(OnReadComplete));//, BookmarkOptions.MultipleResume);
        }

        void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            var ownerCount = BusinessOwnersCount.Get(context);
            var companyID = CompanyID.Get(context);

            var requestApproval = state as RequestApproval;
            context.SetValue<string>(this.Notes, requestApproval.Note);
            context.SetValue<int>(this.ResourceID, requestApproval.ResourceID);

            //context.SetValue<bool>(this.Result, requestApproval.Approved);
            this.Result.Set(context, requestApproval.Approved ? 1 : 0); //NOTE: When setting TResult to bool, I get the error -> Cannot convert object 'True' to type 'System.Int32'

            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Read Approval", System.Diagnostics.TraceLevel.Info)
            {
                Data = 
                        {
                            {"CompanyID", companyID},
                            {"BusinessOwnersCount", ownerCount},
                            {"ApproverResourceID", requestApproval.ResourceID},
                            {"Note", requestApproval.Note},
                            {"Approved", requestApproval.Approved}
                        }
            };
            context.Track(trackingRecord);
        }
    }
}
