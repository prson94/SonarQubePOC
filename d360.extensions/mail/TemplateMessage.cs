using Mandrill;
using Mandrill.Model;
using Microsoft.Azure;
using System.Collections.Generic;

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
            
            var api = new MandrillApi(CloudConfigurationManager.GetSetting("MandrillApiKey"));
            var result = api.Messages.SendTemplateAsync(message, templateID).Result;            
        }

    }
}
