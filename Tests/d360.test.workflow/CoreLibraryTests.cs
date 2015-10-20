using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.workflow;
using System.Activities;
using System.Collections.Generic;
using d360.workflow.models;
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
            api.Messages.SendTemplate(message, templateID);

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
        public void Test_SendEmailActivity()
        {
            var tags = new Dictionary<string, string>();
            tags.Add("approver", "Mike P");
            tags.Add("type", "business term");
            tags.Add("name", "test artifact name");
            tags.Add("description", "a description");
            tags.Add("url", "http://demo.data3sixty.local");
            var activity = new SendEmailActivity
            {
                Body = "The body of the message",
                Subject = "Test subject",
                ToEmail = "mike@data3sixty.com",
                ToName = "Mike Pappas",
                TemplateID = "suggest-new-artifact-approver",
                TemplateTags = tags,
                DisplayName = "Send Test"
            };

            WorkflowInvoker.Invoke(activity);
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
            api.Messages.SendTemplate(message, "suggest-new-artifact-approver");
        }

        [TestMethod]
        public void Test_CertifyArtifact()
        {
            var processor = new Processor();
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("CompanyID", 1);
            dictionary.Add("requestInfo", new CertifyArtifactRequest { ArtifactID = 733, DueDate = DateTime.UtcNow.AddDays(2), StartDate = DateTime.UtcNow });

            var instanceID = processor.CreateNewWorkflowInstance(WorkflowVersionMap.CertifyArtifactIdentity_v1000, dictionary);

            //var instanceID = new Guid("9232f23f-a8fb-4d36-a015-61213e9f9b33");//("4B8EC0FD-842F-49DC-8CCF-B285E3D4FFBB");
            //processor.TerminateWorkflowInstance(instanceID, "Outdated");

            processor.ResumeWorkflowInstance(instanceID, "CertificationFromOwner", new CertificationApproval { ResourceID = 1 });
        }

        [TestMethod]
        public void Test_SuggestNewArtifact()
        {
            var processor = new Processor();
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("CompanyID", 1);
            dictionary.Add("requestInfo", new NewArtifactRequest { Name = "Test Workflow Artifact 2", ArtifactTypeID = 1, Description = "Some description", Fields = new Dictionary<string, object>(), RequestingResourceID = 1, VocabularyID = 4 });

            var instanceID = processor.CreateNewWorkflowInstance(WorkflowVersionMap.SuggestNewArtifactIdentity_v1000, dictionary);
            //var instanceID = new Guid("9232f23f-a8fb-4d36-a015-61213e9f9b33");//("4B8EC0FD-842F-49DC-8CCF-B285E3D4FFBB");
            //processor.TerminateWorkflowInstance(instanceID, "Outdated");
            processor.ResumeWorkflowInstance(instanceID, "ApprovalFromOwner", new RequestApproval { Approved = false, Note = "This item was already requested by someone else.", ResourceID = 1 });
        }
        //ApprovalFromOwner

        [TestMethod]
        public void Test_WorkIssue()
        {
            var processor = new Processor();
            var dictionary = new Dictionary<string, object>();
            dictionary.Add("CompanyID", 4);
            dictionary.Add("CommentID", 28399);

            var instanceID = processor.CreateNewWorkflowInstance(WorkflowVersionMap.WorkIssue_vCurrent, dictionary);
            processor.ResumeWorkflowInstance(instanceID, "Open", new IssueBookmarkModel { ResourceID = 1, Action = "assign", Comment = "I am taking this issue.", ReAssignToResourceObject = "Resource", ReAssignToResourceObjectID = 1 });
            processor.ResumeWorkflowInstance(instanceID, "Assigned", new IssueBookmarkModel { ResourceID = 2, Action = "reassign", Comment = "I am re-assigning this issue.", ReAssignToResourceObject = "Resource", ReAssignToResourceObjectID = 1 });
            //processor.ResumeWorkflowInstance(instanceID, "Assigned", new IssueBookmarkModel { ResourceID = 1, Action = "close", Comment = "I am closing this issue.", ReAssignToResourceObject = "Resource", ReAssignToResourceObjectID = 1 });
        }

        [TestMethod]
        public void Test_TerminateIssueList()
        {
            var processor = new Processor();
            var list = new List<Guid> {
                Guid.Parse("063D0A3A-2871-4FF5-B226-2D628C5AB3E9"),
                Guid.Parse("054BAD68-9776-41D2-8940-E9DF643EFB9B"),
                Guid.Parse("D747B902-E2F7-4015-B3D4-BFDB7A6536B6"),
                Guid.Parse("3451408A-EB95-4028-BA00-7D210BBD0A81"),
                Guid.Parse("2BE77BFB-5566-449A-83A5-7D00FB8A8AAF"),
                Guid.Parse("65C5BA34-884B-4EAD-A908-181E4DE83574"),
                Guid.Parse("D7E0544D-47C2-442D-AC12-C4FBFBC95C3E"),
                Guid.Parse("FD3C4A3D-C9BB-477A-B5CD-BC99C62AF53F"),
                Guid.Parse("326946FA-F2F7-4558-B171-2D4B9C652D99"),
                Guid.Parse("CBD70C87-5E92-457B-A1F7-72174885D267"),
                Guid.Parse("0FAC872E-A987-4D31-A3C9-2908665BC61F"),
                Guid.Parse("2E7BAF93-B99C-4E14-9A47-5FB219E54B3D"),
                Guid.Parse("D6ECE677-091D-4A1F-BFC9-58BA60D6BCA0"),
                Guid.Parse("1B6207A1-7C45-4462-8AE9-AFB35EF5E193"),
                Guid.Parse("7A22AFDC-48FB-4B07-9867-FBE8E27981A2"),
                Guid.Parse("D4497C5E-0518-4B78-B403-9AE14F48409E")
            };
            list.ForEach(g =>
            {
                try
                {
                    processor.TerminateWorkflowInstance(g, "Outdated");
                }
                catch
                {}
            });
        }
    }
}
