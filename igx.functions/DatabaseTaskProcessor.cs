using System;
using Mandrill.Model;
using System.Data;
using Dapper;
using System.Linq;
using System.Collections;
using Microsoft.Azure.WebJobs;
using d360.core.entities;
using d360.core;
using System.Collections.Generic;
using d360.core.queue;
using System.Xml.Linq;

namespace igx.functions.databasetaskprocessor
{    
    public static class DatabaseTaskProcessor
    {
        public static void SendMailToUser(string toName, string toEmail, string subject, string templateID, System.Collections.Generic.Dictionary<string, string> templateTags, string fromName = "Data3Sixty Workflow")
        {
            // Create the email object first, then add the properties.
            var message = new MandrillMessage();

            message.AddTo(toEmail, toName);
            message.FromEmail = "no-reply@data3sixty.com";
            message.FromName = fromName;
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

            //Add the HTML and Text bodies            
            var api = new Mandrill.MandrillApi(CoreFunction.GetConfigValueByKey("MandrillApiKey"));
            var resp = api.Messages.SendTemplateAsync(message, templateID).Result;

            message = null;
            api = null;
        }

        const string functionName = "DatabaseTask_ProcessScheduled";
        const string timerSettings = "*/1 * * * * *";
        const int markitLineageSettingID = 62;


        [FunctionName("DatabaseTaskProcessor")]
        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, System.IO.TextWriter log)
        {
            
        }
    }

    internal class TagSqlModel
    {
        public Guid TagUID { get; set; }
        public string Value { get; set; }
    }

    internal class ResponsibilitySqlModel
    {
        public long AssetID { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
    }
}
