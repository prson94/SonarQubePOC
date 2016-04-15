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

        /// <summary>
        /// Gets a list of open workflow tasks for the current user, based on the workflow type.
        /// </summary>
        /// <returns>A list of workflow tasks.</returns>
        [Route("tasks/types/{id:int}"), HttpGet]
        public HttpResponseMessage GetTasksForUser(int id, int resourceID = -1)
        {
            var userId = resourceID > 0 ? resourceID : Company.CurrentResourceID;

            var workflowType = (WorkflowType)Enum.Parse(typeof(WorkflowType), id.ToString());

            switch (workflowType)
            {
                case WorkflowType.SuggestNewArtifact:
                    var list1 = Company.Query<WorkflowTask1Model>(string.Format(QueryConstants.CurrentUserWorkflow1TaskItem, ""), new { r = userId }).ToList();
                    list1.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list1);
                case WorkflowType.CertifyArtifact:
                    var list2 = Company.Query<WorkflowTask2Model>(string.Format(QueryConstants.CurrentUserWorkflow2TaskItem, ""), new { r = userId }).ToList();
                    list2.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list2);
                case WorkflowType.WorkIssue:
                    var list3 = Company.Query<WorkflowTask3Model>(string.Format(QueryConstants.CurrentUserWorkflow3TaskItem, ""), new { r = userId }).ToList();
                    list3.ForEach(i => {
                        i.ActivityDescription = i.Activity.GetReportTileTypeDescription();
                        i.ActivityName = i.Activity.GetActivityTypeDisplayName();
                        i.WorkflowDescription = workflowType.GetWorkflowTypeDescription();
                        i.WorkflowName = workflowType.GetWorkflowTypeDisplayName();
                    });
                    return Request.CreateResponse(HttpStatusCode.OK, list3);
                case WorkflowType.ChallengeArtifact:
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
        public HttpResponseMessage GetTaskByIDForObjectAndType(WorkflowType workflowType, int objectid, string objecttype) 
        {
            switch (workflowType)
            {
                case WorkflowType.WorkIssue:                    
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
                case WorkflowType.SuggestNewArtifact:
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
                case WorkflowType.CertifyArtifact:
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
                case WorkflowType.WorkIssue:
                    sql = string.Format(QueryConstants.CurrentUserWorkflow3TaskItem, whereSuffix);
                    var model3 = Company.Query<WorkflowTask3Model>(sql, new { r = Company.CurrentResourceID }).SingleOrDefault();
                    if (model3 != null)
                    {
                        model3.WorkflowName = workflow.WorkflowType.GetWorkflowTypeDisplayName();
                        model3.WorkflowDescription = workflow.WorkflowType.GetWorkflowTypeDescription();
                        model3.ActivityName = model3.Activity.GetActivityTypeDisplayName();
                        model3.ActivityDescription = model3.Activity.GetReportTileTypeDescription();

                        return Request.CreateResponse(HttpStatusCode.OK, model3);
                    }
                    break;
                case WorkflowType.ChallengeArtifact:
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
                    case ActivityType.OwnerApproval:
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
                    case ActivityType.OwnerCertification:
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
                    case ActivityType.AssignIssueToPool:
                    case ActivityType.AssignIssueToSelf:
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
                
                var keysToRemove = new List<string>();
                foreach(var k in response.Fields)
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
    }
}
