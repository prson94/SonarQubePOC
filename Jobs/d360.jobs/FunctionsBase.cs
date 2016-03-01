using d360.utils.company;
using Mandrill;
using Mandrill.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace d360.jobs
{
    public class FunctionsBase
    {
        public static void ExecuteActionOnAllCompanies(string actionName, string sql, int timeout)
        {
            CompanyConnectionUtils.ExecuteActionOnAllCompanies(actionName, sql, timeout);
        }

        public static string GetCompanyConnectionString(int companyID)
        {
            return CompanyConnectionUtils.GetCompanyConnectionString(companyID);
        }

        public static SqlConnection GetCompanyConnection(int companyID)
        {
            return CompanyConnectionUtils.GetCompanyConnection(companyID);
        }

        public static List<int> GetActiveCompanyIDs()
        {
            return CompanyConnectionUtils.GetActiveCompanyIDs();
        }

        public static List<int> GetActiveDevelopmentCompanyIDs()
        {
            return CompanyConnectionUtils.GetActiveDevelopmentCompanyIDs();
        }

        public static Dictionary<int, string> GetCompanyDomainPrefixes()
        {
            return CompanyConnectionUtils.GetCompanyDomainPrefixes();
        }

        public static void SendMailToUser(string toName, string toEmail, string subject, string body, string templateID, Dictionary<string, string> templateTags, string fromName = "Data3Sixty Workflow")
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = fromName;
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

            //Add the HTML and Text bodies
            //message.Html = body;
            //message.Text = "Hello World plain text!"; 

            var api = new MandrillApi("XBspYSVRlKva-pXOlDYWEg");
            api.Messages.SendTemplate(message, templateID);

            message = null;
            api = null;
        }
    }
}
