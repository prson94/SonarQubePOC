using System.Activities;
using System;
using System.Xml.Linq;

namespace d360.workflow
{
    public sealed class BuildIssueRequestXml : CodeActivity
    {
        public InArgument<int> CompanyID { get; set; }
        public InArgument<int> CommentID { get; set; }
        public InArgument<int> CommentCreatorResourceID { get; set; }
        public InArgument<DateTime> CommentDateCreated { get; set; }

        public OutArgument<XElement> Xml { get; set; }

        protected override void Execute(CodeActivityContext context)
        {
            int companyID = context.GetValue(this.CompanyID);
            var commentID = context.GetValue(this.CommentID);
            var resourceID = context.GetValue(this.CommentCreatorResourceID);

            var xml = new XElement("fields", 
                new XElement("CompanyID", companyID),
                new XElement("CommentID", commentID),
                new XElement("ResourceID", resourceID)
            );
            context.SetValue<XElement>(this.Xml, xml);
        }
    }
}
