using Mandrill.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandrill;
using d360.core;

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

            var api = new MandrillApi(constants.MANDRILL_API_KEY);

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

            var api = new MandrillApi(constants.MANDRILL_API_KEY);

            await api.Messages.SendAsync(message);
            
        }

    }
}
