using System;
using d360.model;
using System.Net.Http;
using System.Web.Http;
using d360.workflow;
using System.Linq;
using System.Runtime.Serialization;
using d360.core;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities;
using d360.workflow.models;
using System.Net;
using d360.workflow.entities;
using d360.web.Models;
using System.Web.Http.OData;
using System.IO;
using SpreadsheetLight;
using d360.core.entities.Workflow;
using System.Threading.Tasks;
using d360.core.enums.Workflow;
using System.Text;
using Newtonsoft.Json;
using System.Web;
using d360.model.workflow;
using System.Data.Entity;

namespace d360.web.Controllers.Services
{
    [RoutePrefix("services/workflow"), Authorize]
    public class WorkflowController : BaseApiController
    {

        #region DI

        public WorkflowController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        void hydrateTasks(List<WorkflowTask> tasks)
        {
            var Artifacts = new List<int>();
            List<Artifact> artifacts = null;

            var ArtifactTypes = new List<int>();
            List<ArtifactType> artifactTypes = null;

            var Resources = new List<int>();
            List<GlobalReportingResource> resources = null;

            var TaxonomyTypes = new List<int>();
            List<TaxonomyType> taxonomyTypes = null;

            #region Get all the IDs we need to look up

            tasks.ForEach(t => {
                var properties = (
                                 from s in XElement.Parse(t.Data).Elements()
                                 select new { Name = s.Name.LocalName, Value = s.Value }
                                 ).ToList();

                int id = 0;

                properties.ForEach(p => {
                    switch (p.Name)
                    { 
                        case "ArtifactID":
                            id = int.Parse(p.Value);
                            if (!Artifacts.Contains(id)) Artifacts.Add(id);
                            break;
                        case "ArtifactTypeID":
                            id = int.Parse(p.Value);
                            if (!ArtifactTypes.Contains(id)) ArtifactTypes.Add(id);
                            break;
                        case "ResourceID":
                        case "RequestingResourceID":
                            id = int.Parse(p.Value);
                            if (!Resources.Contains(id)) Resources.Add(id);
                            break;
                        case "StartDate":
                            var sd = DateTime.Parse(p.Value);
                            t.Properties.Add("Start Date", sd.ToShortDateString());
                            break;
                        case "EndDate":
                            var ed = DateTime.Parse(p.Value);
                            t.Properties.Add("End Date", ed.ToShortDateString());
                            break;
                        case "DueDate":
                            var dd = DateTime.Parse(p.Value);
                            t.Properties.Add("Due Date", dd.ToShortDateString());
                            break;
                        case "TaxonomyTypeID":
                            id = int.Parse(p.Value);
                            if (!TaxonomyTypes.Contains(id)) TaxonomyTypes.Add(id);
                            break;
                        default:
                            if (!p.Name.StartsWith("FieldType_"))
                            {
                                t.Properties.Add(p.Name, p.Value);
                            }
                            break;
                    }
                });
            });

            #endregion

            #region Look up the data based on the ID lists we got above
            
            if (Artifacts.Count > 0)
                artifacts = Company.Filter<Artifact>(i => Artifacts.Contains(i.ID)).ToList();
            
            if (ArtifactTypes.Count > 0)
                artifactTypes = Company.Filter<ArtifactType>(i => ArtifactTypes.Contains(i.ID)).ToList();
            
            if (Resources.Count > 0)
                resources = Company.Filter<GlobalReportingResource>(i => Resources.Contains(i.ResourceID)).ToList();

            if (TaxonomyTypes.Count > 0)
                taxonomyTypes = Company.Filter<TaxonomyType>(i => TaxonomyTypes.Contains(i.ID)).ToList();

            #endregion

            #region Now hydrate the tasks with the data we got back from DB

            tasks.ForEach(t =>
            {
                t.WorkflowName = t.Workflow.GetWorkflowTypeDisplayName();
                t.WorkflowDescription = t.Workflow.GetWorkflowTypeDescription();
                t.ActivityName = t.Activity.GetActivityTypeDisplayName();
                t.ActivityDescription = t.Activity.GetReportTileTypeDescription();

                var properties = (
                                 from s in XElement.Parse(t.Data).Elements()
                                 select new { Name = s.Name.LocalName, Value = s.Value }
                                 ).ToList();

                properties.ForEach(p =>
                {
                    switch (p.Name)
                    {
                        case "ArtifactID":
                            var a = artifacts.SingleOrDefault(i => i.ID == int.Parse(p.Value));
                            if (a != null)
                            {
                                t.Properties.Add("Artifact", string.Format("<a href='/#/artifacts/{0}/{1}' data-type='Artifact' data-id='{1}' data-context='Preview'>{2}<a>", a.ArtifactTypeID, a.ID, a.Name));
                            }
                            a = null;
                            break;
                        case "ArtifactTypeID":
                            var at = artifactTypes.SingleOrDefault(i => i.ID == int.Parse(p.Value));
                            if (at != null)
                            {
                                t.Properties.Add("Type", string.Format("<a href='/#/artifacts/{0}' data-type='ArtifactType' data-id='{0}' data-context='Preview'>{1}<a>", at.ID, at.Name));
                            }
                            at = null;
                            break;
                        case "ResourceID":
                        case "RequestingResourceID":
                            var r = resources.SingleOrDefault(i => i.ResourceID == int.Parse(p.Value));
                            if (r != null)
                            {
                                var fieldName = (p.Name == "RequestingResourceID") ? "Requestor" : "Resource";
                                t.Properties.Add(fieldName, string.Format("<a href='/#/resources/{0}' data-type='Resource' data-id='{0}' data-context='Preview'>{1} {2}<a>", r.ResourceID, r.FirstName, r.LastName));
                            }
                            r = null;
                            break;
                        case "TaxonomyTypeID":
                            var tt = taxonomyTypes.SingleOrDefault(i => i.ID == int.Parse(p.Value));
                            if (tt != null)
                            {
                                t.Properties.Add("Subject Area", string.Format("<a href='/#/catalogs/{0}' data-type='TaxonomyType' data-id='{0}' data-context='Preview'>{1}<a>", tt.ID, tt.Name));
                            }
                            tt = null;
                            break;
                    }
                });
            });

            #endregion

            #region Destroy

            Artifacts = null;
            artifacts = null;
            ArtifactTypes = null;
            artifactTypes = null;
            Resources = null;
            resources = null;
            TaxonomyTypes = null;
            taxonomyTypes = null;

            #endregion
        }

        /// <summary>
        /// Gets a list of open workflow tasks for the current user.
        /// </summary>
        /// <returns>A list of workflow tasks.</returns>
        [Route("tasks/types/breakdown"), HttpGet]
        public List<WorkflowBreakdown> GetTaskBreakdownForCurrentUser()
        {
            var items = Company.Query<WorkflowBreakdown>(QueryConstants.CurrentUserWorkflowCount, new { r = Company.CurrentResourceID }).ToList();
            items.ForEach(i => { 
                i.WorkflowTypeID = (int)i.Workflow;
                i.WorkflowTypeName = i.Workflow.GetWorkflowTypeDisplayName();
            });
            return items;
        }
        
        [Route("all/issues"), HttpGet]
        public IQueryable GetIssuesForAllUsers()
        {            
            var res = from workflows in Company.WorkflowIssues
                              join comments in Company.Comments on workflows.CommentID equals comments.ID                          
                              from resources in Company.WorkflowResources
                               .Where(o => workflows.WorkflowID == o.WorkflowID && o.IsComplete == false && o.ResourceID == Company.CurrentResourceID)
                               .DefaultIfEmpty()                            
                            select new
                              {
                                  WorkflowID = workflows.WorkflowID,
                                  Issue = comments.Body,
                                  DateStarted = workflows.DateStarted,
                                  DateCompleted = workflows.DateCompleted,
                                  IsCompleted = workflows.IsCompleted,
                                  Name = workflows.Name,
                                  Object = workflows.Object,                                  
                                  AllowAction = resources != null,
                                  RaisedBy = workflows.RaisedBy,
                                  ObjectID = workflows.ObjectID,
                                  RaisedByResourceID = workflows.CreatingResourceID,
                                  Url = workflows.Url,
                                  ActivityName = workflows.IsCompleted ? "Closed" : (resources != null ? "Pending" : "Waiting on user(s)"),
                                  Notes = workflows.Comments,
                                  IssueType = workflows.IssueType,
                                  IssueTypeName = workflows.IssueTypeName,
                                  IssueID = workflows.IssueID,
                                  Criticality = workflows.CriticalityName,
                                  EllapsedDays = workflows.EllapsedDays
                            };

                  return res.Distinct();                  
        }


        [Route("my/issues"), HttpGet]
        public IQueryable GetIssuesForMyUser()
        {
            var res = from workflows in Company.WorkflowIssues
                      join comments in Company.Comments on workflows.CommentID equals comments.ID
                      from resources in Company.WorkflowResources
                       .Where(o => workflows.WorkflowID == o.WorkflowID && o.IsComplete == false && o.ResourceID == Company.CurrentResourceID)
                       .DefaultIfEmpty()
                      where(workflows.CreatingResourceID == Company.CurrentResourceID || resources.ResourceID == Company.CurrentResourceID)
                      select new
                      {
                          WorkflowID = workflows.WorkflowID,
                          Issue = comments.Body,
                          DateStarted = workflows.DateStarted,
                          DateCompleted = workflows.DateCompleted,
                          IsCompleted = workflows.IsCompleted,
                          Name = workflows.Name,
                          Object = workflows.Object,
                          AllowAction = resources != null,
                          RaisedBy = workflows.RaisedBy,
                          ObjectID = workflows.ObjectID,
                          RaisedByResourceID = workflows.CreatingResourceID,
                          Url = workflows.Url,
                          ActivityName = workflows.IsCompleted ? "Closed" : (resources != null ? "Pending" : "Waiting on user(s)"),
                          Notes = workflows.Comments,
                          IssueType = workflows.IssueType,
                          IssueTypeName = workflows.IssueTypeName,
                          IssueID = workflows.IssueID,
                          Criticality = workflows.CriticalityName,
                          EllapsedDays = workflows.EllapsedDays
                      };

            return res.Distinct();
        }

        [Route("all/issues/excel/excel.xls"), HttpGet]
        public HttpResponseMessage GetIssuesForAllUsersExcel(bool all = true)
        {
            IQueryable<dynamic> res = null;

            if (all)
            {
                res = from workflows in Company.WorkflowIssues
                      join comments in Company.Comments on workflows.CommentID equals comments.ID
                      from resources in Company.WorkflowResources
                       .Where(o => workflows.WorkflowID == o.WorkflowID && o.IsComplete == false && o.ResourceID == Company.CurrentResourceID)
                       .DefaultIfEmpty()
                      select new
                      {
                          WorkflowID = workflows.WorkflowID,
                          Issue = comments.Body,
                          DateStarted = workflows.DateStarted,
                          DateCompleted = workflows.DateCompleted,
                          IsCompleted = workflows.IsCompleted,
                          Name = workflows.Name,
                          Object = workflows.Object,
                          AllowAction = resources != null,
                          RaisedBy = workflows.RaisedBy,
                          ObjectID = workflows.ObjectID,
                          RaisedByResourceID = workflows.CreatingResourceID,
                          Url = workflows.Url,
                          ActivityName = workflows.IsCompleted ? "Closed" : (resources != null ? "Pending" : "Waiting on user(s)"),
                          Notes = workflows.Comments,
                          IssueType = workflows.IssueType,
                          IssueTypeName = workflows.IssueTypeName,
                          IssueID = workflows.IssueID,
                          Criticality = workflows.CriticalityName,
                          EllapsedDays = workflows.EllapsedDays
                      };
            }
            else
            {
                res = from workflows in Company.WorkflowIssues
                      join comments in Company.Comments on workflows.CommentID equals comments.ID
                      from resources in Company.WorkflowResources
                       .Where(o => workflows.WorkflowID == o.WorkflowID && o.IsComplete == false && o.ResourceID == Company.CurrentResourceID)
                       .DefaultIfEmpty()
                      where (workflows.CreatingResourceID == Company.CurrentResourceID || resources.ResourceID == Company.CurrentResourceID)
                      select new
                      {
                          WorkflowID = workflows.WorkflowID,
                          Issue = comments.Body,
                          DateStarted = workflows.DateStarted,
                          DateCompleted = workflows.DateCompleted,
                          IsCompleted = workflows.IsCompleted,
                          Name = workflows.Name,
                          Object = workflows.Object,
                          AllowAction = resources != null,
                          RaisedBy = workflows.RaisedBy,
                          ObjectID = workflows.ObjectID,
                          RaisedByResourceID = workflows.CreatingResourceID,
                          Url = workflows.Url,
                          ActivityName = workflows.IsCompleted ? "Closed" : (resources != null ? "Pending" : "Waiting on user(s)"),
                          Notes = workflows.Comments,                          
                          IssueTypeName = workflows.IssueTypeName,                          
                          Criticality = workflows.CriticalityName,
                          EllapsedDays = workflows.EllapsedDays
                      };
            }
                        
            var results = res.Distinct();

            var document = new SLDocument();
            document.AddWorksheet("Items");

            #region Create the list sheet

            #region Header

            var colIndex = 0;

            document.SetCellValue(1, ++colIndex, "Issue");
            document.SetCellValue(1, ++colIndex, "Name");
            document.SetCellValue(1, ++colIndex, "Type");
            document.SetCellValue(1, ++colIndex, "Created By");
            document.SetCellValue(1, ++colIndex, "Created On");
            document.SetCellValue(1, ++colIndex, "Closed On");
            document.SetCellValue(1, ++colIndex, "Status");
            document.SetCellValue(1, ++colIndex, "Closing Notes");
            document.SetCellValue(1, ++colIndex, "Action Type");
            document.SetCellValue(1, ++colIndex, "Criticality");
            document.SetCellValue(1, ++colIndex, "Ellapsed Days");

            #endregion

            int rowIndex = 1;
            foreach (var row in results)
            {
                var dataColIndex = 0;
                rowIndex++;

                document.SetCellValue(rowIndex, ++dataColIndex, row.Issue ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Name ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Object ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.RaisedBy ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.DateStarted.ToShortDateString());
                document.SetCellValue(rowIndex, ++dataColIndex, row.DateCompleted != null ? row.DateCompleted.ToShortDateString(): "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.ActivityName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Notes ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.IssueTypeName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Criticality ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, (row.EllapsedDays ?? "").ToString());
            }

            #endregion


            var stream = new MemoryStream();
            document.SaveAs(stream);
            var len = stream.Length;
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);
            //  result.
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Issues as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }


        /// <summary>
        /// Gets a list of open workflow tasks for the current user, based on the workflow type.
        /// </summary>
        /// <returns>A list of workflow tasks.</returns>
        [Route("tasks/types/{id:int}"), HttpGet]
        public HttpResponseMessage GetTasksForUser(int id, int resourceID = -1)
        {
            var userId = resourceID > 0 ? resourceID : Company.CurrentResourceID;

            var workflowType = (d360.workflow.WorkflowType)Enum.Parse(typeof(d360.workflow.WorkflowType), id.ToString());

            switch (workflowType)
            {
                case d360.workflow.WorkflowType.SuggestNewArtifact:
                    var list1 = Company.Query<WorkflowTask1Model>(string.Format(QueryConstants.CurrentUserWorkflow1TaskItem, ""), new { r = userId }).ToList();
                    list1.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list1);
                case d360.workflow.WorkflowType.CertifyArtifact:
                    var list2 = Company.Query<WorkflowTask2Model>(string.Format(QueryConstants.CurrentUserWorkflow2TaskItem, ""), new { r = userId }).ToList();
                    list2.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list2);
                case d360.workflow.WorkflowType.WorkIssue:
                    var list3 = Company.Query<WorkflowTask3Model>(string.Format(QueryConstants.CurrentUserWorkflow3TaskItem, ""), new { r = userId }).ToList();
                    list3.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                        i.CriticalityName = Enum.GetName(typeof(core.enums.IssueCriticality), i.Criticality);                    
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list3);
                case d360.workflow.WorkflowType.ChallengeArtifact:
                    var list4 = Company.Query<WorkflowTask4Model>(string.Format(QueryConstants.CurrentUserWorkflow4TaskItem, ""), new { r = userId }).ToList();
                    list4.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list4);
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "The Workflow Type you provided is not valid.  No workflows can be found of this type.");
        }

        [Route("tasks/types/{workflowType:int}/{objectid:int}/{objecttype}"), HttpGet]
        public HttpResponseMessage GetTaskByIDForObjectAndType(d360.workflow.WorkflowType workflowType, int objectid, string objecttype) 
        {
            switch (workflowType)
            {
                case d360.workflow.WorkflowType.WorkIssue:                    
                    var list = Company.Query<WorkflowTask3Model>(QueryConstants.CurrentUserWorkflow3SpecificObjectTaskItem, new { r = Company.CurrentResourceID, type = objecttype, id = objectid });

                    foreach (var item in list)
                    {
                        if ((int)item.Activity != 0)
                        {
                            item.ActivityDescription = item.Activity.GetReportTileTypeDescription();
                            item.ActivityName = item.Activity.GetActivityTypeDisplayName();                            
                        }
                        else
                        {
                            item.ActivityName = "Waiting on user(s)...";
                        }
                        item.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        item.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                        item.CriticalityName = Enum.GetName(typeof(core.enums.IssueCriticality), item.Criticality);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, list);             
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Workflow not found");
        }

        /// <summary>
        /// Gets a list of open workflow tasks for the current user.
        /// </summary>
        /// <returns>A list of workflow tasks.</returns>
        [Route("tasks/{id}"), HttpGet]
        public HttpResponseMessage GetTaskByIDForCurrentUser(Guid id) //WorkflowTask 
        {
            var workflow = Company.GetById<Workflow>(id);
            var whereSuffix = string.Format("where W.ID = '{0}'", id.ToString());
            var sql = "";
            switch (workflow.WorkflowType)
            {
                case d360.workflow.WorkflowType.SuggestNewArtifact:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow1TaskItem, whereSuffix);
                    var model1 = Company.Query<WorkflowTask1Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model1 != null)
                    {
                        model1.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model1.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model1.ActivityName = model1.Activity.GetActivityTypeDisplayName();
                        model1.ActivityDescription = model1.Activity.GetReportTileTypeDescription();

                        return Request.CreateResponse(HttpStatusCode.OK, model1);
                    }
                    break;
                case d360.workflow.WorkflowType.CertifyArtifact:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow2TaskItem, whereSuffix);
                    var model2 = Company.Query<WorkflowTask2Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model2 != null)
                    {
                        model2.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model2.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model2.ActivityName = model2.Activity.GetActivityTypeDisplayName();
                        model2.ActivityDescription = model2.Activity.GetReportTileTypeDescription();

                        return Request.CreateResponse(HttpStatusCode.OK, model2);
                    }
                    break;
                case d360.workflow.WorkflowType.WorkIssue:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow3TaskItem, whereSuffix);
                    var model3 = Company.Query<WorkflowTask3Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model3 != null)
                    {
                        model3.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model3.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model3.ActivityName = model3.Activity.GetActivityTypeDisplayName();
                        model3.ActivityDescription = model3.Activity.GetReportTileTypeDescription();
                        model3.CriticalityName = Enum.GetName(typeof(core.enums.IssueCriticality), model3.Criticality);

                        return Request.CreateResponse(HttpStatusCode.OK, model3);
                    }
                    break;
                case d360.workflow.WorkflowType.ChallengeArtifact:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow4TaskItem, whereSuffix);
                    var model4 = Company.Query<WorkflowTask4Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model4 != null)
                    {
                        model4.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model4.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model4.ActivityName = model4.Activity.GetActivityTypeDisplayName();
                        model4.ActivityDescription = model4.Activity.GetReportTileTypeDescription();

                        return Request.CreateResponse(HttpStatusCode.OK, model4);
                    }
                    break;
                case d360.workflow.WorkflowType.SuggestNewArtifactMulti:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow1TaskItem, whereSuffix);
                    var model5 = Company.Query<WorkflowTask5Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model5 != null)
                    {
                        model5.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model5.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model5.ActivityName = model5.Activity.GetActivityTypeDisplayName();
                        model5.ActivityDescription = model5.Activity.GetReportTileTypeDescription();

                        return Request.CreateResponse(HttpStatusCode.OK, model5);
                    }
                    break;
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Workflow not found");
        }

        /// <summary>
        /// Approve or reject the current workflow task for the current user.
        /// </summary>
        /// <returns>A status code.</returns>
        [Route("tasks/{id}"), HttpPost]
        public HttpResponseMessage ActOnTaskForCurrentUser(Guid id, WorkflowRequestModel model)//ApprovalFormModel model)
        {
            HttpResponseMessage response = null;
            var task = Company.Query<WorkflowTask>(QueryConstants.CurrentWorkflowTaskItem, new { r = Company.CurrentResourceID, w = id }).SingleOrDefault();

            string bookmarkName = "";
            Object obj = null;
            if (task != null)
            {
                var processor = new Processor();

                switch (task.Activity)
                {
                    case d360.workflow.ActivityType.OwnerApproval:
                        bookmarkName = "ApprovalFromOwner";
                        //var appModel = model as WorkflowApprovalRequestModel;
                        obj = new RequestApproval
                        {
                            Approved = bool.Parse(model["Approved"]),
                            Note = model["Notes"],
                            ResourceID = Company.CurrentResourceID
                        };
                        try
                        {
                            processor.ResumeWorkflowInstance(id, bookmarkName, obj);
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.Accepted, new {
                                context = "OwnerApprovalWorkflow", action = "edit", id = id.ToString(), type = "confirm", title = "Workflow Task", text =  "Workflow task successfully completed."
                            });
                        }
                        catch (Exception ex)
                        {
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.BadRequest, new
                            {
                                context = "OwnerApprovalWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "error",
                                title = "Workflow Task",
                                text = ex.GetFullExceptionData()
                            });
                        }
                        break;
                    case d360.workflow.ActivityType.OwnerCertification:
                        bookmarkName = "CertificationFromOwner";
                        obj = new CertificationApproval
                        {
                            ResourceID = Company.CurrentResourceID
                        };
                        try
                        {
                            processor.ResumeWorkflowInstance(id, bookmarkName, obj);
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.Accepted, new
                            {
                                context = "OwnerCertificationWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "confirm",
                                title = "Workflow Task",
                                text = "Workflow task successfully completed."
                            });
                        }
                        catch (Exception ex)
                        {
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.BadRequest, new
                            {
                                context = "OwnerCertificationWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "error",
                                title = "Workflow Task",
                                text = ex.GetFullExceptionData()
                            });
                        }
                        break;
                    case d360.workflow.ActivityType.AssignIssueToPool:
                    case d360.workflow.ActivityType.AssignIssueToSelf:
                        var action = model["WorkflowAction"];

                        obj = new IssueBookmarkModel
                        {
                            ResourceID = Company.CurrentResourceID,
                            Action = action,
                            Comment = model["Comment"],
                            ReAssignToResourceObject = "Resource",
                            ReAssignToResourceObjectID = Company.CurrentResourceID
                        };

                        bool okToProceed = false;
                        switch (action) {
                            case "assign":
                                bookmarkName = "Open";
                                okToProceed = true;
                                break;
                            case "reassign":
                                bookmarkName = "Assigned";
                                if (model.ContainsKey("AssignTo"))
                                {
                                    (obj as IssueBookmarkModel).ReAssignToResourceObjectID = int.Parse(model["AssignTo"]);
                                    okToProceed = true;
                                }
                                break;
                            default://case "close":
                                bookmarkName = "Assigned";
                                okToProceed = true;
                                break;
                        }

                        try
                        {
                            if (okToProceed)
                            {
                                processor.ResumeWorkflowInstance(id, bookmarkName, obj);
                                response = Request.CreateResponse<dynamic>(HttpStatusCode.Accepted, new
                                {
                                    context = "IssueWorkflow",
                                    action = "edit",
                                    id = id.ToString(),
                                    type = "confirm",
                                    title = "Workflow Task",
                                    text = "Workflow task successfully completed."
                                });
                            }
                            else
                            {
                                response = Request.CreateResponse<dynamic>(HttpStatusCode.NoContent, new
                                {
                                    context = "IssueWorkflow",
                                    action = "edit",
                                    id = id.ToString(),
                                    type = "error",
                                    title = "Workflow Task",
                                    text = "Workflow task not processed as there was no data available to work with.  Please check your request."
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.BadRequest, new
                            {
                                context = "IssueWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "error",
                                title = "Workflow Task",
                                text = ex.GetFullExceptionData()
                            });
                        }

                        break;

                    case d360.workflow.ActivityType.FinalApproval:
                        bookmarkName = "ApprovalFromOwner";
                        //var appModel = model as WorkflowApprovalRequestModel;
                        obj = new RequestApproval
                        {
                            Approved = bool.Parse(model["Approved"]),
                            Note = " " + (model["Notes"] ?? ""),
                            ResourceID = Company.CurrentResourceID
                        };
                        try
                        {
                            processor.ResumeWorkflowInstance(id, bookmarkName, obj);
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.Accepted, new
                            {
                                context = "OwnerApprovalWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "confirm",
                                title = "Workflow Task",
                                text = "Workflow task successfully completed."
                            });
                        }
                        catch (Exception ex)
                        {
                            response = Request.CreateResponse<dynamic>(HttpStatusCode.BadRequest, new
                            {
                                context = "OwnerApprovalWorkflow",
                                action = "edit",
                                id = id.ToString(),
                                type = "error",
                                title = "Workflow Task",
                                text = ex.GetFullExceptionData()
                            });
                        }
                        break;
                }
            }
            else 
            {
                response = Request.CreateResponse<dynamic>(HttpStatusCode.NotFound, new
                {
                    context = "IssueWorkflow",
                    action = "edit",
                    id = id.ToString(),
                    type = "error",
                    title = "Workflow Task",
                    text = "You either do not have permissions to this work item or have already completed it."
                });
            }

            return response;
        }

        /// <summary>
        /// Gets the status of a given workflow, containing all steps executed as well as assignments.
        /// </summary>
        /// <param name="id">The ID of the workflow record to retrieve status for.</param>
        /// <returns></returns>
        [Route("{id}/status")]
        public WorkflowViewModel GetWorkflowStatus(Guid id)
        {
            WorkflowViewModel response = null;

            var model = Company.Filter<Workflow>(i => i.ID == id, i => i.WorkflowResources, i => i.WorkflowStatuses).SingleOrDefault();

            if (model != null)
            {
                var statuses = model.WorkflowStatuses.OrderBy(i => i.RecordNumber).Select(i => new WorkflowStatusViewModel { Name = i.ActivityName, Date = i.Date, TraceLevel = i.TraceLevel.ToString(), ID = i.ID }).ToList();
                var assignments = (
                                  from wr in model.WorkflowResources
                                  join r in Company.GlobalReportingResources on wr.ResourceID equals r.ResourceID
                                  select new WorkflowResourceViewModel 
                                  { 
                                      ActivityType = wr.Activity, 
                                      ActivityTypeDescription = wr.Activity.GetReportTileTypeDescription(), 
                                      ActivityTypeName = wr.Activity.GetActivityTypeDisplayName(), 
                                      IsComplete = wr.IsComplete, 
                                      ResourceID = wr.ResourceID,
                                      ResourceName = r.FirstName + " " + r.LastName
                                  }
                                  ).ToList();

                response = new WorkflowViewModel
                {
                    DateCompleted = model.DateCompleted,
                    DateStarted = model.DateStarted,
                    ID = model.ID,
                    WorkflowType = model.WorkflowType,
                    WorkflowTypeDescription = model.WorkflowType.GetWorkflowTypeDescription(),
                    WorkflowTypeName = model.WorkflowType.GetWorkflowTypeDisplayName(),
                    Assignments = assignments,
                    Steps = statuses,
                    Fields = XElement.Parse(model.Data).Elements().Select(el => new Property { Name = el.Name.LocalName, Value = el.Value }).ToList()
                };

                var fieldName = "ArtifactID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var artifactID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var artifact = Company.GetById<Artifact>(artifactID);
                    if (artifact != null)
                    {
                        response.Fields.Add(new Property { Name = "Artifact", Value = artifact.Name });
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                    }
                    artifact = null;
                }

                fieldName = "ArtifactTypeID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var artifactTypeID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var artifactType = Company.GetById<ArtifactType>(artifactTypeID);
                    if (artifactType != null)
                    {
                        response.Fields.Add(new Property { Name = "Type", Value = artifactType.Name });
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                    }
                    artifactType = null;
                }

                fieldName = "RequestingResourceID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var resourceID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var resource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == resourceID).SingleOrDefault();
                    if (resource != null)
                    {
                        response.Fields.Add(new Property { Name = "Requestor", Value = resource.FirstName + " " + resource.LastName });
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                    }
                    resource = null;
                }

                fieldName = "ResourceID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var resourceID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var resource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == resourceID).SingleOrDefault();
                    if (resource != null)
                    {
                        response.Fields.Add(new Property { Name = "Requestor", Value = resource.FirstName + " " + resource.LastName });
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                    }
                    resource = null;
                }

                fieldName = "TaxonomyTypeID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var taxonomyTypeID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var taxonomyType = Company.GetById<TaxonomyType>(taxonomyTypeID);
                    if (taxonomyType != null)
                    {
                        response.Fields.Add(new Property { Name = "TaxonomyType", Value = taxonomyType.Name });
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                    }
                    taxonomyType = null;
                }

                fieldName = "IssueID";
                if (response.Fields.Any(i => i.Name == fieldName))
                {
                    var IssueID = int.Parse(response.Fields.Single(i => i.Name == fieldName).Value);
                    var issue = Company.GetById<Issue>(IssueID, i=>i.IssueType);
                    if (issue != null)
                    {
                        response.Fields.Add(new Property { Name = "Issue Type", Value = issue.IssueType.Name });
                        response.Fields.Add(new Property { Name = "Criticality", Value = Enum.GetName(typeof(core.enums.IssueCriticality),issue.Criticality) });
                        var fields = Company.GetFieldRelationsByObject(SystemObjects.Issue, IssueID).OrderBy(x => x.SortOrder).ToList();
                        foreach (var field in fields)
                    	{
                            response.Fields.Add(new Property { Name = field.FriendlyName, Value = field.FormattedValue });
                        }   
                        response.Fields.RemoveAll(i => i.Name == fieldName);
                        response.Fields.RemoveAll(i => i.Name == "IssueType");
                    }                    
                }
                

                var keysToRemove = new List<string>();
                keysToRemove.Add("CompanyID");
                keysToRemove.Add("CommentID");
                foreach (var k in response.Fields)
                {
                    if (k.Name.StartsWith("FieldType_"))
                    {
                        keysToRemove.Add(k.Name);
                    }
                }
                keysToRemove.ForEach(k => { response.Fields.RemoveAll(i => i.Name == k); });
            }

            return response;
        }

        [Route("diagram/{id:int}")]
        public WorkflowDiagramModel GetWorkflowDiagram(int id)
        {
            var nodes = Company.Query<WorkflowDiagramNode>(QueryConstants.WorkflowDiagramNodes, new { id }).ToList();
            var links = Company.Query<WorkflowDiagramLink>(QueryConstants.WorkflowDiagramLinks, new { id }).ToList();
            var name = Company.Query<string>(@"select name from workflow.[type] where id = @id", new { id }).ToList().First().ToString();
            var type = Company.WorkflowTypes.Find(id);
            var @event = Company.WorkflowEventRegistrations.Single(e => e.TypeID == id);

            nodes.ForEach(n =>
            {
                n.SettingsObject = XmlToDynamic(n.Settings, false);
                n.FieldsObject = XmlToDynamic(n.Fields);
            });

            links.ForEach(l =>
            {
                l.ConditionObject = XmlToDynamic(l.Condition);
            });

            @event.ConditionObject = XmlToDynamic(@event.Condition);
            @event.SettingsObject = XmlToDynamic(@event.Settings, false);

            return new WorkflowDiagramModel
            {
                Nodes = nodes,
                Links = links,
                Type = type,
                Event = @event
            };
        }

        [HttpPost, Route("SubmitWorkflowForm/{itemId:int}/{itemStepId:int}")]
        public HttpResponseMessage SubmitWorkflowForm(int itemId, int itemStepId, List<WorkflowFormModelField> model)
        {
            try
            {
                var item = Company.WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();
                var itemStepsModel = Company.WorkflowItemSteps.Where(x => x.ID == itemStepId).FirstOrDefault();

                if(itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "CANNOT FIND THE ITEM STEP FOR THE SPEICIFIED PARAMETERS");
                }

                StringBuilder sb = new StringBuilder();

                sb.Append("<fields><form>");

                foreach (var field in model)
                {                    
                    var val = field.Value != null ? field.Value.ToString() : "";

                    if (field.FieldType == WorkflowFormModelFieldType.boolean) {
                        var boolVal = (val ?? "").ToUpper() == "TRUE" ? "TRUE" : "FALSE";
                        sb.Append($"<field id=\"{field.ID}\" label=\"{field.Label}\" value=\"{boolVal}\" fieldtype=\"{field.FieldType.ToString().ToLower()}\"></field>");
                    }
                    else
                        sb.Append($"<field id=\"{field.ID}\" label=\"{field.Label}\" value=\"{val}\" fieldtype=\"{field.FieldType.ToString().ToLower()}\"></field>");                    
                }
                
                sb.Append("</form></fields>");

                if (itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unable to find item step");
                }

                itemStepsModel.Fields = sb.ToString();
                itemStepsModel.CompletedOn = DateTime.UtcNow;
                itemStepsModel.CompletedBy = Company.CurrentResourceID;

                Company.Entry(itemStepsModel).State = System.Data.Entity.EntityState.Modified;
                Company.SaveChanges();

                var @object = (SystemObjects)Enum.Parse(typeof(SystemObjects), item.Object);
                
                var obj = Company.GetObjectDetail(@object, item.ObjectID);

                var type  = (SystemObjects)Enum.Parse(typeof(SystemObjects), obj.Type);

                //complete step and go to transitions
                Company.MarkStepAsCompleteAndContinue(itemStepsModel, itemId, new core.queue.EventObjectInfo { Object = @object, ObjectID = item.ObjectID, ObjectTypeID = obj.TypeID, ObjectType = type } );

                return Request.CreateResponse(HttpStatusCode.Accepted, itemStepsModel);
            }            
            catch (Exception ex)
            {                
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);                
            }
        }

        [Route("form/{typeID:int}/{itemStepID:int}"), HttpGet]
        public async Task<HttpResponseMessage> GetWorkflowForm(int typeID, int itemStepID)
        {
            var itemStep = Company.WorkflowItemSteps.Where(x => x.ID == itemStepID).Include(x=>x.Item).FirstOrDefault();

            if(itemStep == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "INVALID WORKFLOW ITEM ID,  CANNOT FIND WORKFLOWITEMSTEP WITH SPECIFIED ID.");
            }
            
            string sql = @"
                    SELECT vs.[Fields]      
                      FROM 
	                    [workflow].[VersionStep] vs
                        inner join [workflow].[itemstep] wis on(vs.id = wis.stepid)
                     where wis.id = @id
                ";

            var xml = (await Company.QueryAsync<string>(sql, new { id = itemStepID })).FirstOrDefault();

            if (string.IsNullOrEmpty(xml))
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "INVALID WORKFLOW FORM DEFINITION,  FORM XML IS NULL.");
            }
            
            var desc = (string)XElement.Parse(xml).Element("form").Attribute("description");
            var title = (string)XElement.Parse(xml).Element("form").Attribute("title");

            if(string.IsNullOrEmpty(xml))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Workflow with specified version step id not found");
                
            List<WorkflowFormModelField> properties = (
                                 from s in XElement.Parse(xml).Element("form").Elements()
                                 select new WorkflowFormModelField{ Value = (string)s.Attribute("value"), ID = (string)s.Attribute("id"), Label = (string)s.Attribute("label"), FieldType = (WorkflowFormModelFieldType)Enum.Parse( typeof(WorkflowFormModelFieldType), (string)s.Attribute("type")) }
                                 ).ToList();


            var details = Company.GetObjectDetail(itemStep.Item.Object, itemStep.Item.ObjectID);
            
            //parse the xml to get the form info

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, new
            {
                Fields = properties,
                Title = title ?? "",
                Description = desc ?? "",
                IsCompleted = itemStep.CompletedOn.HasValue,
                ObjectName = details.Name,
                ObjectType = itemStep.Item.Object,
                ObjectID = itemStep.Item.ObjectID
            });
        }

        [Route("activitytypes"), HttpGet]
        public List<core.enums.Workflow.ActivityTypeInfo> GetActivityTypes()
        {
            return d360.core.enums.Workflow.WorkflowActivityType.EmailNotification.GetList().ToList();
        }

        [Route("changetypes"), HttpGet]
        public List<ChangeTypeInfo> GetChangeTypes()
        {
            return ChangeType.Add.GetList();
        }

        [Route("transitiontypes"), HttpGet]
        public List<TransitionTypeInfo> GetTransitionTypes()
        {
            return TransitionType.Always.GetList();
        }

        [Route("types"), HttpGet]
        public HttpResponseMessage GetWorkflowTypes()
        {
            string sql = @"select t.ID
                    ,t.Name
                    ,t.CreatedOn
					,coalesce(rc.FirstName + ' ' + rc.LastName, '') as CreatedBy
                    ,t.UpdatedOn
					,coalesce(ru.FirstName + ' ' + ru.LastName, '') as UpdatedBy
                    ,e.ChangeType
                    ,d.Name as TypeName,
					case when t.PublishedVersionID is not null then
						'Version ' + cast(v.Version as varchar) + ' Published'
					else
						'Unpublished'
					end as Published
                from workflow.type t
                inner join workflow.eventregistration e on e.typeid = t.id
                inner join cache.objectdetails d on d.object = e.object and d.objectid= e.objectid 
				left join workflow.version v on v.id = t.publishedversionid
				left join reporting.Global_Resource rc on rc.ResourceID = t.CreatedBy
				left join reporting.Global_Resource ru on ru.ResourceID = t.UpdatedBy
				where t.Deleted = 0              
        ";

            var types = Company.Query<dynamic>(sql).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);

        }

        [Route("types/{objectID:int}/{objectType}"), HttpGet]
        public HttpResponseMessage GetWorkflowTypesForObject(int objectID, string objectType)
        {
            string sql = @"select t.ID
                    ,t.Name
                    ,t.CreatedOn
                    ,t.UpdatedOn
                    ,e.ChangeType
                    ,'' as 'ConditionText'
                    ,e.[Condition] as 'Condition'
                    ,v.Version as Version
                from workflow.type t
                join workflow.eventregistration e on e.typeid = t.id    
                join workflow.[version] v on t.id = v.typeid            
                where 
                    e.objectid = @id and e.[object] = @type
            ";

            var types = Company.Query<dynamic>(sql, new { id = objectID, type = new Dapper.DbString { Value = objectType, IsFixedLength = true, Length = 50, IsAnsi = true } }).ToList();

            foreach (var type in types)
            {
                try
                {
                    type.ConditionText = WorkflowRegistrationCriteriaProcessor.ToPlainText(Company, type.Condition);
                }
                catch { }
            }

            return Request.CreateResponse(HttpStatusCode.OK, types);

        }

        [Route("items/{typeId:int}"), HttpGet]
        public HttpResponseMessage GetItemsForWorkflow(int typeId)
        {
            string sql = @"select
	                        i.[object] as 'Object'
	                        ,i.objectid as 'ObjectId'
	                        ,i.updatedon as 'UpdatedOn'
	                        ,i.completedon as 'CompletedOn'
                            ,i.numberofevents as 'NumberOfEvents'
	                        ,od.name as 'Name'
                            ,od.NgUrl as 'Url'
                            
                          from
	                        [workflow].[version] v
	                        inner join [workflow].item i on v.id = i.versionid
	                        inner join [cache].objectdetails od on i.objectid = od.objectid and i.[object] = od.[object]                            
                          where 
	                        v.typeid = @id
            ";

            var types = Company.Query<dynamic>(sql, new { id = typeId }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("item/detail/{itemId:int}"), HttpGet]
        public HttpResponseMessage GetItemDetail(int itemId)
        {
            var item = Company.WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();

            if (item == null)
                Request.CreateErrorResponse(HttpStatusCode.NotFound, "Cannot find the specified workflow instance.");

            // get the itemsteps for this workflow instance

            var itemSteps = Company.WorkflowItemSteps.Where(x => x.ItemID == itemId);
            var steps = Company.WorkflowVersionSteps.Where(x => x.VersionID == item.VersionID);
            var workflow = Company.WorkflowTypes.Where(x => x.ID == item.Version.TypeID).FirstOrDefault();

            var objectDetails = Company.GetObjectDetail(item.Object, item.ObjectID);

            return Request.CreateResponse(HttpStatusCode.OK, 
                new
                {
                    Item = item,
                    Workflow = workflow,
                    ItemSteps = itemSteps,
                    Steps = steps,
                    ObjectDetails = objectDetails                    
                });
        }

        [Route("item/details/{workflowId:int}/{objectType}/{objectId:int}"), HttpGet]
        public HttpResponseMessage GetItemDetailsForWorkflow(int workflowId, string objectType, int objectId)
        {
            string sql = @"		select
			                        vs.name as 'Name',
			                        istep.startedOn as 'StartedOn',
                                    R.FirstName + ' ' + R.LastName as StartedBy, 
			                        istep.completedon as 'CompletedOn',
                                    Rc.FirstName + ' ' + Rc.LastName as CompletedBy,
                                    vs.ActivityType as ActivityType,
                                    vs.StepType as StepType
                                from
			                        [workflow].item i
	                                inner join [workflow].itemstep istep on (i.id = istep.itemid)
                                    inner join [workflow].version v on (i.versionid = v.id)
			                        inner join [workflow].versionstep vs on (vs.id = istep.stepid)
                                    inner join [reporting].[Global_Resource] R on R.ResourceID = istep.startedby
                                    left join [reporting].[Global_Resource] Rc on Rc.ResourceID = istep.completedby
		                        where
			                        i.[object] = @typename and i.[objectid] = @id and v.typeid = @workflowId;
            ";

            var types = Company.Query<dynamic>(sql, new { workflowId = workflowId, id = objectId, typename = objectType }).ToList();
            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("objecttypes"), HttpGet]
        public HttpResponseMessage GetObjectTypes()
        {
            string sql = @"select 'ArtifactType|' + cast(id as varchar) as value, id, 'ArtifactType' as [type], 'Artifact Type :: ' +  Name as [name] from artifacttype
                            union all
                            select 'RuleType|' + cast(id as varchar) as value, id, 'RuleType' as [type], 'Rule Type :: ' + Name as [name] from ruletype
                            union all
                            select 'PolicyType|' + cast(id as varchar) as value, id, 'PolicyType' as [type], 'Policy Type :: ' + Name as [name] from policytype
                            union all
                            select 'TaxonomyType|' + cast(id as varchar) as value, id, 'TaxonomyType' as [type], 'Model Type :: ' + Name as [name] from taxonomytype
                            union all
                            select 'IssueType|' + cast(id as varchar) as value, id, 'IssueType' as [type], 'Action Type :: ' + Name as [name] from issuetype";

            var types = Company.Query<dynamic>(sql);
            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("type/{id:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowType(int id)
        {
            var type = Company.WorkflowTypes.Find(id);
            if (type == null || type.Deleted)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type id {id} could not be found");

            var @event = Company.WorkflowEventRegistrations.Single(e => e.TypeID == id);

            @event.ConditionObject = XmlToDynamic(@event.Condition);
            @event.SettingsObject = (@event.Settings == null) ? JsonConvert.DeserializeObject("{}") : JsonConvert.DeserializeObject(JsonConvert.SerializeXNode(XDocument.Parse(@event.Settings)));

            return Request.CreateResponse(HttpStatusCode.OK, new { Type = type, Event = @event });
        }

        [Route("fieldtypes/{type}/{id:int}"), HttpGet]
        public HttpResponseMessage GetFieldTypes(int id, string type)
        {
            var fields = Company.FieldTypes.Where(f => f.Object == type && f.ObjectID == id).ToList();
            string[] excludedTypes = { "ComplexRelationLookup", "Password", "Html", "Link", "FilteredLookup", "FusionLookup" };

            fields = fields.Where(f => !excludedTypes.Contains(f.Type)).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, fields);
        }
        
        [Route("diagram/save"), HttpPost]
        public HttpResponseMessage PostWorkflowDiagramModel(WorkflowDiagramModel model)
        {

            bool newVersion = false; //TODO: logic to determine if new version is needed
            int versionID = 0;

            try
            {
                if (model.Type != null)
                {
                    if (model.Type.ID < 1)
                    {
                        var type = new d360.core.entities.Workflow.Type();
                        var version = new WorkflowVersion();

                        type.ID = 0;
                        type.CreatedBy = Company.CurrentResourceID;
                        type.CreatedOn = DateTime.UtcNow;
                        type.UpdatedBy = Company.CurrentResourceID;
                        type.UpdatedOn = DateTime.UtcNow;
                        type.Name = model.Type.Name;

                        Company.Add(type);
                        Company.SaveChanges();

                        model.Type.ID = type.ID;

                        version.ID = 0;
                        version.TypeID = type.ID;
                        version.CreatedBy = Company.CurrentResourceID;
                        version.CreatedOn = DateTime.UtcNow;
                        version.UpdatedBy = Company.CurrentResourceID;
                        version.UpdatedOn = DateTime.UtcNow;
                        version.Version = 1;

                        Company.Add(version);
                        Company.SaveChanges();
                        versionID = version.ID;
                    }
                    else
                    {
                        var type = Company.WorkflowTypes.Find(model.Type.ID);
                        type.Name = model.Type.Name;
                        type.UpdatedOn = DateTime.UtcNow;
                        type.UpdatedBy = Company.CurrentResourceID;

                        
                        versionID = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First().ID;

                        Company.SaveChanges();
                    }

                    if (model.Event != null)
                    {
                        if (model.Event.ID < 1)
                        {
                            var @event = new WorkflowEventRegistration();

                            @event.ID = 0;
                            @event.Object = model.Event.Object;
                            @event.ObjectID = model.Event.ObjectID;
                            @event.TypeID = model.Type.ID;
                            @event.ChangeType = model.Event.ChangeType;
                            @event.Condition = JsonConvert.DeserializeXNode(model.Event.Condition).ToString();
                            @event.Settings = JsonConvert.DeserializeXNode(model.Event.Settings).ToString();


                            Company.Add(@event);
                            Company.SaveChanges();
                        }
                        else
                        {
                            var @event = Company.WorkflowEventRegistrations.Find(model.Event.ID);

                            @event.Object = model.Event.Object;
                            @event.ObjectID = model.Event.ObjectID;
                            @event.TypeID = model.Type.ID;
                            @event.ChangeType = model.Event.ChangeType;

                            @event.Condition = JsonConvert.DeserializeXNode(model.Event.Condition).ToString();
                            @event.Settings = JsonConvert.DeserializeXNode(model.Event.Settings).ToString();

                            Company.SaveChanges();

                        }
                    }


                    Dictionary<int, int> keyMapping = new Dictionary<int, int>();

                    if (model?.Nodes?.Count > 0)
                    {
                        //TODO: parse nodes and add
                        model.Nodes.ForEach(n =>
                        {

                            int id = 0;
                            int.TryParse(n.Key, out id);

                            if (id < 0)
                            {
                                var step = new WorkflowVersionStep();
                                step.ID = 0;
                                step.Name = n.Name ?? "";
                                step.StepType = n.StepType;
                                step.ActivityType = n.ActivityType;
                                step.XPosition = n.XPosition;
                                step.YPosition = n.YPosition;
                                step.VersionID = versionID;
                                step.Settings = JsonConvert.DeserializeXNode(n.Settings).ToString();

                                if (string.IsNullOrEmpty(n.Fields))
                                    step.Fields = null;
                                else
                                    step.Fields = JsonConvert.DeserializeXNode(n.Fields).ToString();

                                Company.Add(step);
                                Company.SaveChanges();
                                keyMapping.Add(id, step.ID);
                            }
                            else if (id > 0)
                            {
                                //modify

                                var node = Company.WorkflowVersionSteps.Find(id);

                                if (node != null)
                                {
                                    node.ActivityType = n.ActivityType;
                                    node.Name = n.Name ?? "";
                                    node.StepType = n.StepType;
                                    node.XPosition = n.XPosition;
                                    node.YPosition = n.YPosition;
                                    node.VersionID = versionID;
                                    node.Settings = JsonConvert.DeserializeXNode(n.Settings).ToString();

                                    if (string.IsNullOrEmpty(n.Fields))
                                        node.Fields = null;
                                    else
                                        node.Fields = JsonConvert.DeserializeXNode(n.Fields).ToString();

                                    keyMapping.Add(id, id);
                                }
                            }
                        });
                        Company.SaveChanges();
                    }

                    if (model?.Links?.Count > 0)
                    {
                        //TODO: parse links and add
                        model.Links.ForEach(l =>
                        {
                            int from = 0;
                            int to = 0;

                            int.TryParse(l.FromKey, out from);
                            int.TryParse(l.ToKey, out to);

                            bool fromNew = (from < 0);
                            bool toNew = (to < 0);


                            if (fromNew || toNew)
                            {
                                var link = new WorkflowVersionStepTransition();

                                link.FromVersionStepID = keyMapping[from];
                                link.ToVersionStepID = keyMapping[to];
                                link.Name = l.Name ?? "";
                                link.TransitionType = l.TransitionType;
                                link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString(); 
                                link.FromPortID = l.FromPortID;
                                link.ToPortID = l.ToPortID;

                                Company.Add(link);
                            }
                            else
                            {
                                var link = Company.WorkflowVersionStepTransitions.Single(v => v.FromVersionStepID == from && v.ToVersionStepID == to);

                                if (link != null)
                                {
                                    link.Name = l.Name ?? "";
                                    link.TransitionType = l.TransitionType;
                                    link.FromPortID = l.FromPortID;
                                    link.ToPortID = l.ToPortID;
                                    link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString();
                                }
                            }
                        });

                        Company.SaveChanges();
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, model.Type.ID);

            } catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [Route("type/{id:int}/delete")]
        public HttpResponseMessage DeleteWorkflow(int id)
        {
            var type = Company.WorkflowTypes.Find(id);

            if (type == null)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type ID {id} could not be found");

            if (type.Deleted)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type ID {id} is already deleted");

            type.Deleted = true;
            type.UpdatedOn = DateTime.UtcNow;
            type.UpdatedBy = Company.CurrentResourceID;
            Company.SaveChanges();

            //var @event = Company.WorkflowEventRegistrations.Single(e => e.TypeID == id);

            //var versions = Company.WorkflowVersions.Where(v => v.TypeID == id).ToList();
            //var versionSteps = Company.WorkflowVersionSteps.Where(s => versions.Select(v => v.ID).Contains(s.ID)).ToList();
            //var versionStepTransitions = Company.WorkflowVersionStepTransitions.Where(t => versions.Select(v => v.ID).Contains(t.FromVersionStepID) || versions.Select(v => v.ID).Contains(t.ToVersionStepID)).ToList();

            //Company.WorkflowVersionStepTransitions.RemoveRange(versionStepTransitions);
            //Company.WorkflowVersionSteps.RemoveRange(versionSteps);
            //Company.WorkflowVersionStepTransitions.RemoveRange(versionStepTransitions);

            //Company.WorkflowVersions.RemoveRange(versions);

            //Company.Delete(@event);
            //Company.Delete(type);

            //Company.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, id);
        }

        #region Helper Methods

        private dynamic XmlToDynamic(string xml, bool omitRootElement = true)
        {
            return string.IsNullOrEmpty(xml) ? JsonConvert.DeserializeObject("{}") : JsonConvert.DeserializeObject(JsonConvert.SerializeXNode(XElement.Parse(xml), Formatting.None, omitRootElement));
        }

        #endregion
    }
}
