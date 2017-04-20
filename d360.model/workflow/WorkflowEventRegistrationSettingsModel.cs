using d360.core.enums.Workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.model.workflow
{
    public class WorkflowEventRegistrationSettingsModel
    {
        public bool SendAggregateEmail { get; set; }
        public string EmailHeader { get; set; }
        public string EmailMessageTemplate { get; set; }
        public int ScheduleInterval { get; set; }
        public EmailTaskRecipientType RecipientType { get; set; }
        public string SpecificUser { get; set; }

        public static WorkflowEventRegistrationSettingsModel Parse(string xml)
        {
            var settingsXml = XElement.Parse(xml);
            var messageRecipientType = EmailTaskRecipientType.Initiator;

            var scheduledDays = 1;
            if (settingsXml.Element("ScheduleInterval") != null)
            {
                int.TryParse(settingsXml.Element("ScheduleInterval").Value, out scheduledDays);
            }

            bool shouldSendAggregateEmail = false;
            if (settingsXml.Element("SendAggregateEmail") != null)
            {
                bool.TryParse(settingsXml.Element("SendAggregateEmail").Value, out shouldSendAggregateEmail);
            }

            var emailTemplate = "";
            if (settingsXml.Element("MessageBodyTemplate") != null)
            {
                emailTemplate = settingsXml.Element("MessageBodyTemplate").Value;
            }

            var emailHeader = "";
            if (settingsXml.Element("MessageSubjectTemplate") != null)
            {
                emailHeader = settingsXml.Element("MessageSubjectTemplate").Value;
            }

            if (settingsXml.Element("MessageRecipientType") != null)
            {
                messageRecipientType = (EmailTaskRecipientType)Enum.Parse(typeof(EmailTaskRecipientType), settingsXml.Element("MessageRecipientType").Value);
            }

            var specificUser = "";

            if (settingsXml.Element("MessageToUser") != null)
                specificUser = settingsXml.Element("MessageToUser").Value;

            return new WorkflowEventRegistrationSettingsModel
            {
                ScheduleInterval = scheduledDays,
                EmailHeader = emailHeader,
                EmailMessageTemplate = emailTemplate,
                SendAggregateEmail = shouldSendAggregateEmail,
                RecipientType = messageRecipientType,
                SpecificUser = specificUser
            };
        }
    }
}
