using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
using d360.workflow.models;
using System.Activities.Tracking;

namespace d360.workflow
{

    public sealed class AddIssueBookmark : NativeActivity<int>
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<string> BookmarkName { get; set; }

        public OutArgument<int> ResourceID { get; set; }
        public OutArgument<string> Action { get; set; }
        public OutArgument<string> Comment { get; set; }
        public OutArgument<string> ReAssignToResourceObject { get; set; }
        public OutArgument<int?> ReAssignToResourceObjectID { get; set; }
        
        // NativeActivity derived activities that do asynchronous operations by calling 
        // one of the CreateBookmark overloads defined on System.Activities.NativeActivityContext 
        // must override the CanInduceIdle property and return true.
        protected override bool CanInduceIdle
        {
            get { return true; }
        }

        protected override void Execute(NativeActivityContext context)
        {
            //int companyID = context.GetValue(this.CompanyID);
            var bookmarkName = context.GetValue(this.BookmarkName);

            context.CreateBookmark(bookmarkName, new BookmarkCallback(OnReadComplete));//, BookmarkOptions.MultipleResume);
        }

        void OnReadComplete(NativeActivityContext context, Bookmark bookmark, object state)
        {
            var companyID = CompanyID.Get(context);

            var model = state as IssueBookmarkModel;
            context.SetValue<string>(this.Action, model.Action);
            context.SetValue<string>(this.Comment, model.Comment);
            context.SetValue<string>(this.ReAssignToResourceObject, model.ReAssignToResourceObject);
            context.SetValue<int?>(this.ReAssignToResourceObjectID, model.ReAssignToResourceObjectID);
            context.SetValue<int>(this.ResourceID, model.ResourceID);

            this.Result.Set(context, 1);

            var trackingRecord = new CustomTrackingRecord(context.WorkflowInstanceId, "Add Issue Bookmark", System.Diagnostics.TraceLevel.Info)
            {
                Data =
                        {
                            {"CompanyID", companyID},
                            {"Action", model.Action},
                            {"Comment", model.Comment},
                        }
            };
            context.Track(trackingRecord);
        }
    }
}
