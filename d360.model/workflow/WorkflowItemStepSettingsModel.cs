using System;
using System.Collections.Generic;
using System.Xml.Linq;

using d360.core.enums.Workflow;

namespace d360.model.workflow
{
    public class WorkflowItemStepSettingModel
    {
        private static readonly string RESPONSIBILITY_TYPE_ID = "ResponsibilityTypeID";
        private static readonly string FORM_RESPONSE_TYPE = "FormResponseType";
        private static readonly string WAIT_FOR_ALL = "WaitForAllTransitions";
        private static readonly string FORM_SHOULD_EMAIL_USERS = "SendFormEmail";
        private static readonly string STORED_PROC_ID = "ProcedureID";
        private static readonly string EMAIL_MESSAGE_USER = "MessageToUser";
        private static readonly string EMAIL_MESSAGE_GROUP = "MessageToGroup";
        private static readonly string EMAIL_RECIPIENT_TYPE = "MessageRecipientType";
        private static readonly string EMAIL_MESSAGE_BODY = "MessageBodyTemplate";
        private static readonly string EMAIL_MESSAGE_SUBJECT = "MessageSubjectTemplate";
        private static readonly string EMAIL_INCLUDE_RESPONSES = "IncludePreviousFormResponses";
        private static readonly string EMAIL_SEND_DEFAULT = "SendToDefaultUsers";
        private static readonly string MISSING_SUBJECT_VALUE = "Data360 - Workflow Email notification (missing subject)";
        private static readonly string MISSING_BODY_VALUE = "Data360 - Workflow Email (missing body).  You are receiving this email due to a Data360 workflow with an email task.  The task has been improperly configured so it doesnt have any email content";
        private static readonly string FIELD_UPDATE_SETTINGS = "FieldUpdate";
        private static readonly string RELATIONSHIP_UPDATE_SETTINGS = "RelationshipUpdate";
        private static readonly string FIELD_SETTINGS = "Field";
        private static readonly string RELATIONSHIP_SETTINGS = "Relationship";
        private static readonly string HTTP_REQUEST_SETTINGS = "HTTPRequest";
        private static readonly string HTTP_RESPONSE_SETTINGS = "HTTPResponse";

        public string SubjectTemplate { get; set; }
        
        public string BodyTemplate { get; set; }

        public bool ShouldIncludeFormResponses { get; set; }
        
        public bool SendToDefaultUsers { get; set; } = true;

        public bool FormShouldSendEmail { get; set; }

        public int ResponsibilityTypeID { get; set; }
        
        public FormResponseType ResponseType { get; set; }

        public bool WaitForAllTransitions { get; set; }

        public int StoredProcedureID { get; set; }

        public EmailTaskRecipientType RecipientType { get; set; }

        public string SpecificUser { get; set; }

        public Guid RecipientGroup { get; set; }

        public List<WorkflowFieldUpdateSettings> FieldUpdateSettings { get; set; }

        public List<WorkflowRelationshipUpdateSettings> RelationshipUpdateSettings { get; set; }
        
        public WorkflowHttpRequestSettingsModel HttpRequestSettings { get; set; }
        
        public WorkflowHttpResponseSettingsModel HttpResponseSettings { get; set; }

        public static WorkflowItemStepSettingModel ParseXml(string root)
        {
            XElement xml = null;
            if (!string.IsNullOrEmpty(root))
            {
                xml = XElement.Parse(root);
            }

            return ParseXml(xml);
        }

        public static WorkflowItemStepSettingModel ParseXml(XElement root)
        {
            int responsibilityTypeID = -1;
            FormResponseType responseType = FormResponseType.FirstResponse;
            bool waitForAll = false;
            bool formShouldEmailUsers = true;
            int storedProcedureID = -1;
            EmailTaskRecipientType messageRecipientType = EmailTaskRecipientType.Initiator;
            string specificUser = "";
            Guid recipientGroup = Guid.Empty;
            bool includeFormResponses = false;
            bool sendToDefaultUsers = true;
            string messageSubject = "";
            string messageBody = "";
            List<WorkflowFieldUpdateSettings> fieldUpdateSettings = new List<WorkflowFieldUpdateSettings>();
            List<WorkflowRelationshipUpdateSettings> relationshipUpdateSettings = new List<WorkflowRelationshipUpdateSettings>();
            WorkflowHttpRequestSettingsModel httpRequestSettings = new WorkflowHttpRequestSettingsModel();
            WorkflowHttpResponseSettingsModel httpResponseSettings = new WorkflowHttpResponseSettingsModel();

            if (root != null)
            {

                if (root.Element(RESPONSIBILITY_TYPE_ID) != null)
                {
                    int.TryParse(root.Element(RESPONSIBILITY_TYPE_ID).Value, out responsibilityTypeID);
                }

                if (root.Element(FORM_RESPONSE_TYPE) != null)
                {
                    responseType = (FormResponseType)Enum.Parse(typeof(FormResponseType), root.Element(FORM_RESPONSE_TYPE).Value);
                }

                if (root.Element(WAIT_FOR_ALL) != null)
                {
                    bool.TryParse(root.Element(WAIT_FOR_ALL).Value, out waitForAll);
                }

                if (root.Element(FORM_SHOULD_EMAIL_USERS) != null)
                {
                    bool.TryParse(root.Element(FORM_SHOULD_EMAIL_USERS).Value, out formShouldEmailUsers);
                }

                if (root.Element(STORED_PROC_ID) != null)
                {
                    int.TryParse(root.Element(STORED_PROC_ID).Value, out storedProcedureID);
                }

                if (root.Element(EMAIL_MESSAGE_USER) != null)
                {
                    specificUser = root.Element(EMAIL_MESSAGE_USER).Value;
                }

                if (root.Element(EMAIL_MESSAGE_GROUP) != null)
                {
                    Guid.TryParse(root.Element(EMAIL_MESSAGE_GROUP).Value, out recipientGroup);
                }

                if (root.Element(EMAIL_RECIPIENT_TYPE) != null)
                {
                    if (!Enum.TryParse<EmailTaskRecipientType>(root.Element(EMAIL_RECIPIENT_TYPE).Value, out messageRecipientType))
                    {
                        messageRecipientType = EmailTaskRecipientType.None;
                    }
                }

                if (root.Element(EMAIL_MESSAGE_SUBJECT) != null)
                {
                    messageSubject = root.Element(EMAIL_MESSAGE_SUBJECT).Value;
                }

                if (root.Element(EMAIL_MESSAGE_BODY) != null)
                {
                    messageBody = root.Element(EMAIL_MESSAGE_BODY).Value;
                }

                if (root.Element(EMAIL_INCLUDE_RESPONSES) != null)
                {
                    includeFormResponses = (root.Element(EMAIL_INCLUDE_RESPONSES).Value ?? "").ToUpper() == "TRUE";
                }

                if (root.Element(EMAIL_SEND_DEFAULT) != null)
                {
                    sendToDefaultUsers = (root.Element(EMAIL_SEND_DEFAULT).Value ?? "TRUE").ToUpper() == "TRUE";
                }

                if (root.Element(FIELD_UPDATE_SETTINGS) != null)
                {
                    foreach (XElement field in root.Element(FIELD_UPDATE_SETTINGS).Elements(FIELD_SETTINGS))
                    {
                        fieldUpdateSettings.Add(WorkflowFieldUpdateSettings.ParseXml(field));
                    }
                }

                if (root.Element(RELATIONSHIP_UPDATE_SETTINGS) != null)
                {
                    foreach (XElement field in root.Element(RELATIONSHIP_UPDATE_SETTINGS).Elements(RELATIONSHIP_SETTINGS))
                    {
                        relationshipUpdateSettings.Add(WorkflowRelationshipUpdateSettings.ParseXml(field));
                    }
                }

                if (root.Element(HTTP_REQUEST_SETTINGS) != null)
                {
                    httpRequestSettings = WorkflowHttpRequestSettingsModel.ParseXml(root.Element(HTTP_REQUEST_SETTINGS));
                }

                if (root.Element(HTTP_RESPONSE_SETTINGS) != null)
                {
                    httpResponseSettings = WorkflowHttpResponseSettingsModel.ParseXml(root.Element(HTTP_RESPONSE_SETTINGS));
                }
            }

            return new WorkflowItemStepSettingModel
            {
                ResponseType = responseType,
                ResponsibilityTypeID = responsibilityTypeID,
                WaitForAllTransitions = waitForAll,
                FormShouldSendEmail = formShouldEmailUsers,
                StoredProcedureID = storedProcedureID,
                SpecificUser = specificUser,
                RecipientGroup = recipientGroup,
                RecipientType = messageRecipientType,
                ShouldIncludeFormResponses = includeFormResponses,
                SendToDefaultUsers = sendToDefaultUsers,
                SubjectTemplate = messageSubject ?? MISSING_SUBJECT_VALUE,
                BodyTemplate = messageBody ?? MISSING_BODY_VALUE,
                FieldUpdateSettings = fieldUpdateSettings,
                RelationshipUpdateSettings = relationshipUpdateSettings,
                HttpRequestSettings = httpRequestSettings,
                HttpResponseSettings = httpResponseSettings,
            };
        }
    }
}
