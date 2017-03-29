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
        public static async Task SendMessage(string subject, string toEmail, string toName, string content)
        {
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);            
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty";

            // Add the message properties.

            message.Subject = subject;
            message.Text = content;

            var api = new MandrillApi(constants.MANDRILL_API_KEY);

            await api.Messages.SendAsync(message);
            
        }

    }
}
