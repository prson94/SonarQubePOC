using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mandrill.Model;
using Mandrill;
using d360.core;

namespace d360.extensions.mail
{
    public static class TemplateMessage
    {
        public static void SendMessage(string subject, string toEmail, string toName,
                Dictionary<string, string> templateTags, string templateID
            )
        {            
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty";

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
