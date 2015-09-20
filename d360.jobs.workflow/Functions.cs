using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Xml.Linq;
using d360.core.queue;
using Newtonsoft.Json;
using Dapper;
using d360.core.entities;
using System.Diagnostics;
using d360.workflow;
using d360.workflow.models;

namespace d360.jobs.workflow
{
    public class Functions: FunctionsBase
    {
        public static void ProcessActionsQueueMessage([QueueTrigger("d3s-workflow")] string message)
        {
            var processor = new Processor();

            var obj = JsonConvert.DeserializeObject<WorkflowObject>(message);

            //var cnn = GetCompanyConnection(obj.CompanyID);
            //cnn.Open();

            switch (obj.To)
            {
                case WorkflowAction.SuggestNewArtifact:
                    #region
                    //cnn.Close();
                    var identity = WorkflowVersionMap.SuggestNewArtifactIdentity_v1000;
                    if (obj.Arguments == null) obj.Arguments = new Dictionary<string, object>();
                    obj.Arguments.Add("CompanyID", obj.CompanyID);
                    obj.Arguments.Add("requestInfo", new NewArtifactRequest { Name = "Test Artifact", Description = "Some description", Fields = new Dictionary<string,object>(), RequestingResourceID = 1, VocabularyID = 1 });
                    //obj.Arguments.Add("HasAuthorityActed", false);
                    //obj.Arguments.Add("MaxRetries", 3);
                    //obj.Arguments.Add("TimeoutHours", 8);
                    processor.CreateNewWorkflowInstance(identity, obj.Arguments);
                    break;
                    #endregion
            }

            //cnn.Dispose();
        }
    }
}
