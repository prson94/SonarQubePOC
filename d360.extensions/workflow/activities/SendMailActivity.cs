using d360.core;
using Mandrill;
using Mandrill.Model;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace d360.extensions.workflow.activities
{
    public class SendMailActivity : IWorkflowActivity
    {
        public int ID { get { return 1; } }

        public string Name { get { return "Send Mail Activity"; } }

        //public XElement Fields
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }

        //    set
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public XElement Settings
        {
            get
            {
                return new XElement("settings",
                    new XElement("MessageSubjectTemplate", MessageSubjectTemplate),
                    new XElement("MessageBodyTemplate", MessageBodyTemplate)
                );
            }

            set
            {
                MessageSubjectTemplate = value.Element("MessageSubjectTemplate").Value;
                MessageBodyTemplate = value.Element("MessageBodyTemplate").Value;
            }
        }

        public string UserFullName { get; set; }

        public string UserEmail { get; set; }

        public string MessageSubjectTemplate { get; set; }

        public string MessageBodyTemplate { get; set; }

        public Dictionary<string, string> MessageTokens { get; set; }

        public void Execute(string settings, bool isTest = false)
        {
            var message = new MandrillMessage();

            message.AddTo("mike@data3sixty.com", "Pappas");//message.AddTo(UserEmail, UserFullName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty";

            var subject = MessageSubjectTemplate;
            var body = MessageBodyTemplate;
            foreach (var k in MessageTokens.Keys)
            {
                subject.Replace($"[{k}]", MessageTokens[k]);
                body.Replace($"[{k}]", MessageTokens[k]);
            }

            message.Subject = subject;
            message.Html = body;
            message.AutoText = true;

            message.TrackOpens = false;
            message.TrackClicks = false;

            var api = new MandrillApi(constants.MANDRILL_API_KEY);
            var result = api.Messages.SendAsync(message).Result;
        }
    }
}
