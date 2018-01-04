using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Activities;
using System.Collections.Generic;
using Mandrill.Model;
using Mandrill;

namespace d360.test.workflow
{
    [TestClass]
    public class CoreLibraryTests
    {
        public void SendMailToUser(string toName, string toEmail, string subject, string body, string templateID, Dictionary<string, string> templateTags)
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty Workflow";
            message.Subject = subject;

            message.TrackOpens = false;
            message.TrackClicks = false;

            var tags = new Dictionary<string, object>();
            if (templateTags != null)
            {
                foreach (var k in templateTags.Keys)
                {
                    message.AddRcptMergeVars(toEmail, k, templateTags[k]);
                }
            }

            var api = new MandrillApi("XBspYSVRlKva-pXOlDYWEg");
            var result = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }

        [TestMethod]
        public void Test_SendCertifyManyArtifactsEmail()
        {
            var tags = new Dictionary<string, string>();
            tags.Add("user", "Michael Pappas");
            tags.Add("count", "255");
            tags.Add("appUrl", string.Format("https://{0}.data3sixty.com", "demo"));
            tags.Add("dueDate", DateTime.Now.ToShortDateString());
            SendMailToUser("Michael Pappas", "mike@data3sixty.com", "Data3Sixty - Time to Certify", "", "certify-artifacts-request", tags);
        }
        
        [TestMethod]
        public void SendEmailViaMandrill()
        {
            var message = new MandrillMessage();// SendGridMessage();

            var toEmail = "mike@data3sixty.com";

            message.AddTo(toEmail, "Mike Pappas");
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = "Data3Sixty Workflow";

            message.Subject = "test subject";

            message.TrackOpens = false;//message.DisableOpenTracking();
            message.TrackClicks = false; //message.DisableClickTracking();

            message.AddRcptMergeVars(toEmail, "requestor", "Mike Pappas");
            message.AddRcptMergeVars(toEmail, "type", "business term");
            message.AddRcptMergeVars(toEmail, "name", "Test term 1");
            message.AddRcptMergeVars(toEmail, "description", "Desc");
            message.AddRcptMergeVars(toEmail, "request_url", "http://www.cnn.com/#");

            var api = new MandrillApi("XBspYSVRlKva-pXOlDYWEg");
            var result = api.Messages.SendTemplateAsync(message, "suggest-new-artifact-approver").Result;
        }        
    }
}
