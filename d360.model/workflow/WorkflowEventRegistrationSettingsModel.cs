using System;
using System.Xml.Linq;

using d360.core.enums.Workflow;

namespace d360.model.workflow
{
    public class WorkflowEventRegistrationSettingsModel
    {
        public bool SendAggregateEmail { get; set; }
        
        public string EmailHeader { get; set; }
        
        public string EmailMessageTemplate { get; set; }
        
        public int ScheduleInterval { get; set; }
        
        public int ScheduleDays { get; set; }
        
        public ScheduleRunType ScheduleType { get; set; }
        
        public EmailTaskRecipientType RecipientType { get; set; }
        
        public string SpecificUser { get; set; }

        public enum ScheduleRunType
        {
            Daily = 'd',
            Hourly = 'h'
        }

        public static WorkflowEventRegistrationSettingsModel Parse(string xml)
        {
            XElement settingsXml = XElement.Parse(xml);
            EmailTaskRecipientType messageRecipientType = EmailTaskRecipientType.Initiator;

            int scheduledInterval = 1;

            if (settingsXml.Element("ScheduleInterval") != null)
            {
                int.TryParse(settingsXml.Element("ScheduleInterval").Value, out scheduledInterval);
            }

            int scheduledDays = 127; //All days
            
            if (settingsXml.Element("ScheduleDays") != null)
            {
                int.TryParse(settingsXml.Element("ScheduleDays").Value, out scheduledDays);
            }

            ScheduleRunType scheduledType = (settingsXml.Element("ScheduleType") != null &&
                settingsXml.Element("ScheduleType").Value.ToLower() == ((char)ScheduleRunType.Hourly).ToString())
                    ? ScheduleRunType.Hourly
                    : ScheduleRunType.Daily; //default


            bool shouldSendAggregateEmail = false;

            if (settingsXml.Element("SendAggregateEmail") != null)
            {
                bool.TryParse(settingsXml.Element("SendAggregateEmail").Value, out shouldSendAggregateEmail);
            }

            string emailTemplate = "";

            if (settingsXml.Element("MessageBodyTemplate") != null)
            {
                emailTemplate = settingsXml.Element("MessageBodyTemplate").Value;
            }

            string emailHeader = "";

            if (settingsXml.Element("MessageSubjectTemplate") != null)
            {
                emailHeader = settingsXml.Element("MessageSubjectTemplate").Value;
            }

            if (settingsXml.Element("MessageRecipientType") != null)
            {
                messageRecipientType = (EmailTaskRecipientType)Enum.Parse(typeof(EmailTaskRecipientType), settingsXml.Element("MessageRecipientType").Value);
            }

            string specificUser = "";

            if (settingsXml.Element("MessageToUser") != null)
            {
                specificUser = settingsXml.Element("MessageToUser").Value;
            }

            return new WorkflowEventRegistrationSettingsModel
            {
                ScheduleInterval = scheduledInterval,
                ScheduleDays = scheduledDays,
                ScheduleType = scheduledType,
                EmailHeader = emailHeader,
                EmailMessageTemplate = emailTemplate,
                SendAggregateEmail = shouldSendAggregateEmail,
                RecipientType = messageRecipientType,
                SpecificUser = specificUser
            };
        }

        public DateTime GetNextExecution(DateTime? lastExecuted)
        {
            DateTime nextRun;

            if (!lastExecuted.HasValue)
            {
                nextRun = DateTime.UtcNow;
            }
            else
            {
                if (ScheduleType == ScheduleRunType.Hourly)
                {
                    nextRun = lastExecuted.GetValueOrDefault().AddHours(ScheduleInterval);
                }
                else
                {
                    nextRun = lastExecuted.GetValueOrDefault().AddDays(ScheduleInterval);
                }
            }

            //If nextRun is not a valid day, add/hours days until it is
            if (((int)Math.Pow(2, (int)nextRun.DayOfWeek) & ScheduleDays) == 0)
            {
                if (ScheduleType == ScheduleRunType.Hourly)
                {
                    //Add hours to push past midnight, if that does not resolve to a valid day, we can move in 1 day increments
                    int hourOffset = 24 - nextRun.Hour;
                    nextRun = nextRun.AddHours(hourOffset);
                }
                while (((int)Math.Pow(2, (int)nextRun.DayOfWeek) & ScheduleDays) == 0)
                {
                    nextRun = nextRun.AddDays(1);
                }
            }

            //Subtract a minute to prevent drifting
            nextRun = nextRun.AddMinutes(-1);

            return nextRun;
        }
    }
}
