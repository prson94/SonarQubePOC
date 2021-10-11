using Mandrill;
using Mandrill.Model;
using Microsoft.Azure;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Configuration;

namespace d360.extensions.mail
{
    public static class SimpleMessage
    {
        public static async Task SendMessage(string fromName, string subject, string toEmail, string toName, string content, bool useHtml = false)
        {
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = fromName;

            // Add the message properties.
            message.TrackClicks = false;
            message.TrackOpens = false;

            message.Subject = subject;
            if (!useHtml)
                message.Text = content;
            else
                message.Html = content;

            GetMandrillsubAccount(ref message);

            var api = new MandrillApi(ConfigurationManager.AppSettings["MandrillApiKey"]);

            await api.Messages.SendAsync(message);

        }

        public static async Task SendMessage(string subject, string toEmail, string toName, string content, bool useHtml = false, string fromEmail = "no-reply@data3sixty.com", string fromName = "Data3Sixty Workflow")
        {
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = fromEmail;
            message.FromName = fromName;

            // Add the message properties.
            message.TrackClicks = false;
            message.TrackOpens = false;

            message.Subject = subject;
            if (!useHtml)
                message.Text = content;
            else
                message.Html = content;

            GetMandrillsubAccount(ref message);


            var api = new MandrillApi(ConfigurationManager.AppSettings["MandrillApiKey"]);

            await api.Messages.SendAsync(message);

        }

        public static void SendMessage(string subject, string toEmail, string toName,
        Dictionary<string, string> templateTags, string templateID
    )
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data360";

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

            GetMandrillsubAccount(ref message);

            var api = new MandrillApi(ConfigurationManager.AppSettings["MandrillApiKey"]);
            var result = api.Messages.SendTemplateAsync(message, templateID).Result;
            if (result == null || result.Count < 1)
            {
                //...
            }
        }

        private static void GetMandrillsubAccount(ref MandrillMessage message)
        {
            var subaccount = ConfigurationManager.AppSettings["MandrillSubAccount"];

            if (subaccount != null && subaccount.Trim() != string.Empty)
            {
                message.Subaccount = subaccount;
            }
        }
    }
}
