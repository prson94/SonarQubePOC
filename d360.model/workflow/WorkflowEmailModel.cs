using d360.core.enums.Workflow;
using System;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowEmailModel
    {
        private static string MISSING_SUBJECT_VALUE = "Data3Sixty - Workflow Email notification (missing subject)";
        private static string MISSING_BODY_VALUE = "Data3Sixty - Workflow Email (missing body).  You are receiving this email due to a Data3Sixty workflow with an email task.  The task has been improperly configured so it doesnt have any email content";

        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }

        public EmailTaskRecipientType RecipientType { get; set; }

        public string SpecificUser { get; set; }

        public bool ShouldIncludeFormResponses { get; set; }

        public static WorkflowEmailModel ParseFromXml(XElement xml)
        {
            if (xml == null) throw new Exception("INVALID XML SPECIFIED");

            var subject = xml.Element("MessageSubjectTemplate").Value;
            var body = xml.Element("MessageBodyTemplate").Value;
            var includeFormResponses = false;

            var specificUser = "";

            if (xml.Element("MessageToUser") != null)
                specificUser = xml.Element("MessageToUser").Value;            

            var messageRecipientType = EmailTaskRecipientType.Initiator;

            if (xml.Element("MessageRecipientType") != null)
            {
                messageRecipientType = (EmailTaskRecipientType)Enum.Parse(typeof(EmailTaskRecipientType), xml.Element("MessageRecipientType").Value);
            }

            if (xml.Element("IncludePreviousFormResponses") != null)
            {
                includeFormResponses = (xml.Element("IncludePreviousFormResponses").Value ?? "").ToUpper() == "TRUE";
            }

            return new WorkflowEmailModel
            {
                SubjectTemplate = subject ?? MISSING_SUBJECT_VALUE,
                BodyTemplate = body ?? MISSING_BODY_VALUE,
                RecipientType = messageRecipientType,
                SpecificUser = specificUser,
                ShouldIncludeFormResponses = includeFormResponses
            };
        }

    }
}
