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

            //context.WorkflowInstanceId
            
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();// SendGridMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty Workflow";

            // Add the message properties.
            //message.From = new MailAddress("no-reply@data3sixty.com", "Data3Sixty Workflow");

            // Add multiple addresses to the To field.
            //List<String> recipients = new List<String>
            //{
            //    string.Format(@"{0} <{1}>", toName, toEmail)
            //};

            //message.AddTo(recipients);

            message.Subject = subject;

            //message.EnableFooter("text", "html");

            message.TrackOpens = false;//message.DisableOpenTracking();
            //message.DisableUnsubscribe();
            message.TrackClicks = false; //message.DisableClickTracking();

            //if (!string.IsNullOrEmpty(templateID))
            //{
            //    //message.EnableTemplateEngine(templateID);
            var tags = new Dictionary<string, object>();
            if (templateTags != null)
            {
                foreach (var k in templateTags.Keys)
                {
                    message.AddRcptMergeVars(toEmail, k, templateTags[k]);
                    //message.AddSubstitution(k, new List<string>() { templateTags[k] });
                }
            }
            //}
            //else 
            //{
           
            //}

            //Add the HTML and Text bodies
            //message.Html = body;
            //message.Text = "Hello World plain text!"; 

            var api = new MandrillApi(constants.MANDRILL_API_KEY);
            api.Messages.SendTemplate(message, templateID);
            //var credential = new NetworkCredential(constants.SMTP_USERNAME, constants.SMTP_PASSWORD);
            //var transport = new Web(credential);
            //transport.Deliver(message);
        }
    }
}
