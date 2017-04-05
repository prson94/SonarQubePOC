using d360.core;
using d360.utils.company;
using Mandrill;
using Mandrill.Model;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace d360.jobs
{
    public class FunctionsBase
    {
        static TelemetryClient _AITelemetryClient;
        public static TelemetryClient AITelemetryClient {
            get
            {
                if(_AITelemetryClient == null)
                {
                    TelemetryConfiguration.Active.InstrumentationKey = "26a323ed-2f37-400b-8938-84cbf4bb13df";
                    
                    _AITelemetryClient = new TelemetryClient();
                }
                return _AITelemetryClient;
            }            
        }

        public static void AITrackEvent(string eventName, IDictionary<string,string> properties = null)
        {            
            AITelemetryClient.TrackEvent(eventName, properties);
        }

        public static void AITrackException(string jobName, Exception e, string companyId = "")
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["WebJob"] = jobName;
            if(!string.IsNullOrEmpty(companyId)) properties["CompanyId"] = companyId;
            AITelemetryClient.TrackException(e, properties);

            AIFlush();
        }

        public static void AITrackJobCompletedNoErrors(string jobName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["WebJob"] = jobName;

            AITelemetryClient.TrackEvent("Job Completed Successfully", properties);

            AIFlush();
        }
                
        public static void AITrackJobStart(string jobName)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["WebJob"] = jobName;

            AITelemetryClient.TrackEvent("Job Started", properties);
        }

        public static void AITrackRequest(string name, TimeSpan elapsedTime)
        {
            AITelemetryClient.TrackRequest(name, DateTime.Now, elapsedTime, "", true);
        }
        

        public static void AIFlush()
        {
            AITelemetryClient.Flush();
        }

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

            var api = new MandrillApi(constants.MANDRILL_API_KEY);
            var ret = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }
    }
}
