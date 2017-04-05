using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;
//using SendGrid;
using System.Net.Mail;
using System.Net;
using d360.core;
using Mandrill.Model;
using Mandrill;

namespace d360.workflow
{

    public sealed class SendEmailActivity : CodeActivity
    {
        // Define an activity input argument of type string
        public InArgument<string> Subject { get; set; }
        public InArgument<string> Body { get; set; }
        public InArgument<string> ToEmail { get; set; }
        public InArgument<string> ToName { get; set; }

        public InArgument<string> TemplateID { get; set; }
        public InArgument<Dictionary<string, string>> TemplateTags { get; set; }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the runtime value of the Text input argument
            string subject = context.GetValue(this.Subject);
            string body = context.GetValue(this.Body);
            string toEmail = context.GetValue(this.ToEmail);
            string toName = context.GetValue(this.ToName);

            string templateID = context.GetValue(this.TemplateID);
            var templateTags = context.GetValue(this.TemplateTags);

            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty Workflow";

            message.Subject = subject;


            message.TrackOpens = false;
            message.TrackClicks = false;

            if (templateTags != null)
            {
                foreach (var k in templateTags.Keys)
                {
                    message.AddRcptMergeVars(toEmail, k, templateTags[k]);
                }
            }

            var api = new MandrillApi(constants.MANDRILL_API_KEY);
            var result = api.Messages.SendTemplateAsync(message, templateID).Result;
        }
    }
}
