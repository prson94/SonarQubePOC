using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandrill.Model;
using Mandrill;


namespace d360.extensions.mail
{
    public static class TemplateMessage
    {
        public static void SendMessage(string subject, string toEmail, string toName,
                Dictionary<string, string> templateTags, string templateID
            )
        {
            
            //context.WorkflowInstanceId

            // Create the email object first, then add the properties.
            var message = new MandrillMessage();// SendGridMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty";

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

            var api = new MandrillApi("XBspYSVRlKva-pXOlDYWEg");
            api.Messages.SendTemplate(message, templateID);
            //var credential = new NetworkCredential(constants.SMTP_USERNAME, constants.SMTP_PASSWORD);
            //var transport = new Web(credential);
            //transport.Deliver(message);
        }

    }
}
