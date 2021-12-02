using System;
using d360.model;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using d360.core;
using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities;
using System.Net;
using d360.web.Models;
using System.IO;
using SpreadsheetLight;
using d360.core.entities.Workflow;
using System.Threading.Tasks;
using d360.core.enums.Workflow;
using System.Text;
using Newtonsoft.Json;
using d360.model.workflow;
using System.Data.Entity;
using System.Text.RegularExpressions;
using Microsoft.Web.Http;
using Newtonsoft.Json.Linq;
using d360.core.queue;
using Dapper;
using d360.core.enums;
using System.Web.Http.Description;
using System.Xml.Serialization;
using d360.core.helpers;
using d360.model.DataAccessLayer;
using Resources;
using d360.core.Models;

namespace d360.web.Controllers.Services
{
    [ApiVersionNeutral, RoutePrefix("services/workflow"), Authorize, ApiExplorerSettings(IgnoreApi = true)]
    public class WorkflowController : BaseApiController
    {

        #region DI

        public WorkflowController(ICommunityContext community, ICompanyContext company, ISettingsRepository settingsRepository)
            : base(community, company, settingsRepository)
        {
        }

        #endregion

        IEnumerable<dynamic> getIssues(int? resourceID)
        {
            var sql = string.Format(@"
select		distinct
            null as WorkflowID
			,wi.ID as WorkflowItemID
            ,c.Body
		    ,I.CommentID as CommentID
			,I.CreatedBy as RaisedByResourceID
			,wi.StartedOn as DateStarted
			,wi.CompletedOn as DateCompleted
            ,case when wi.CompletedOn is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted
			,'' as Step
			,coalesce(D.ObjectID, T.ObjectID) as ObjectID
			,coalesce(D.DisplayValue, T.[Name]) as [Name]
			,coalesce(D.[Object], T.[Object]) as [Object]
			,coalesce(DUrl.[Url], TUrl.[Url]) as [Url]
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,'' as Notes					
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,case when wi.CompletedOn is null then datediff(day,wi.StartedOn,GetUtcDate()) else datediff(day, wi.StartedOn, wi.CompletedOn) end as EllapsedDays
	        ,case 
                when wi.CompletedOn is not null then 'Closed'
		        else
			        case cast(coalesce(IA.ResourceObjectID, 0) as bit)

                        when 1 then 'Pending'
				        else 'Waiting on user(s)'

                    end

            end as ActivityName
from	    Issue I
			inner join [workflow].item wi on (wi.[object] = 'Issue' and wi.[objectid] = i.id)
            inner join workflow.itemstep si on si.itemid = wi.id
			inner join IssueType IT on (I.IssueTypeID = IT.ID)							
			left join AssetDetail D on D.[Object] = I.[Object] and D.ObjectID = I.ObjectID
			outer apply [dbo].[GetAssetUrlById](D.ID) DUrl
			left join AssetType T on T.[Object] = I.[Object] and T.ObjectID = I.ObjectID
			outer apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl
			left outer join reporting.Global_Resource R on R.ResourceID = I.CreatedBy
			left outer join Comment C on C.ID = I.CommentID
            left join workflow.ItemAssignment IA on IA.ItemID = wi.ID and IA.ResourceObject = 'Resource' {0}
order by wi.StartedOn desc",
            resourceID.HasValue ? $"and IA.ResourceObjectID = {resourceID.Value}" : ""
            );


            return Company.Query<dynamic>(sql);
        }

        [Route("all/issues"), HttpGet]
        public HttpResponseMessage GetIssuesForAllUsers()
        {
            return Request.CreateResponse(HttpStatusCode.OK, getIssues(null));
        }


        [Route("my/issues"), HttpGet]
        public HttpResponseMessage GetIssuesForMyUser()
        {
            return Request.CreateResponse(HttpStatusCode.OK, getIssues(Company.CurrentResourceID));
        }

        [Route("all/issues/excel/excel.xls"), HttpGet]
        public HttpResponseMessage GetIssuesForAllUsersExcel(bool all = true)
        {
            var results = all ? getIssues(null) : getIssues(Company.CurrentResourceID);

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

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
                document.SetCellValue(rowIndex, ++dataColIndex, row.DateCompleted != null ? row.DateCompleted.ToShortDateString() : "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.ActivityName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Notes ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.IssueTypeName ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, (row.EllapsedDays ?? "").ToString());
            }

            #endregion


            var stream = new MemoryStream();
            document.SaveAs(stream);
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);

            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Issues as of {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [HttpGet, Route("workflowmonitor/filter/definition")]
        public HttpResponseMessage GetFilerDefinition()
        {
            var filterValues = new List<string>() { "Business Asset","Technical Asset", "Rule", "Policy", "Model", "Action", "Relationship"}.OrderBy(x => x).ToList();

            var filterColumns = new List<GridFilterColumn>();
            filterColumns.Add(new GridFilterColumn { text = "Asset", datafield = "Asset", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Type", datafield = "Type", filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = filterValues, columntype = GridColumn.COLUMN_TYPE_COMBO });
            filterColumns.Add(new GridFilterColumn { text = "Type Name", datafield = "TypeName", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Started", datafield = "StartedOn", filtertype = GridColumn.FILTER_TYPE_DATE, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Completed", datafield = "CompletedOn", filtertype = GridColumn.FILTER_TYPE_DATE, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Assigned To", datafield = "AssignedTo", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Initiator", datafield = "Initiator", filtertype = GridColumn.FILTER_TYPE_STRING, columntype = GridColumn.COLUMN_TYPE_STRING });
            filterColumns.Add(new GridFilterColumn { text = "Status", datafield = "Status", filtertype = GridColumn.FILTER_TYPE_LIST, filteritems = new List<string> { "Complete", "Pending" }, columntype = GridColumn.COLUMN_TYPE_COMBO });

            return Request.CreateResponse(HttpStatusCode.OK, filterColumns.OrderBy(x => x.text).ToList());

        }

        [Route("issue/type/{objectid:int}/{objecttype}"), HttpGet]
        public HttpResponseMessage GetTaskByIDForObjectAndType(int objectid, string objecttype)
        {
            var sql = @"
select		distinct 
            null as WorkflowID
			,wi.ID as WorkflowItemID
            ,coalesce(c.Body,DD.Value) as Body
		    ,I.CommentID
			,I.CreatedBy as RaisedByResourceID
			,wi.StartedOn as DateStarted
			,wi.CompletedOn as DateCompleted
            ,case when wi.CompletedOn is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted
			,'' as Step
			,coalesce(D.ObjectID, T.ObjectID) as ObjectID
			,coalesce(D.DisplayValue, T.[Name]) as [Name]
			,coalesce(D.[Object], T.[Object]) as [Object]
			,coalesce(DUrl.[Url], TUrl.[Url]) as [Url]
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,'' as Notes					
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,case when wi.CompletedOn is null then datediff(day,wi.StartedOn,GetUtcDate()) else datediff(day, wi.StartedOn, wi.CompletedOn) end as EllapsedDays
	        ,case 
                when wi.CompletedOn is not null then 'Closed'
		        else
			        case cast(coalesce(IA.ResourceObjectID, 0) as bit)

                        when 1 then 'Pending'
				        else 'Waiting on user(s)'

                    end

            end as ActivityName
from	    Issue I
			inner join [workflow].item wi on (wi.[object] = 'Issue' and wi.[objectid] = i.id) and I.[object] = @obj and I.[objectid] = @id
			inner join IssueType IT on (I.IssueTypeID = IT.ID)						
			left join AssetDetail D on D.[Object] = I.[Object] and D.ObjectID = I.ObjectID
			outer apply [dbo].[GetAssetUrlById](D.ID) DUrl
			left join AssetType T on T.[Object] = I.[Object] and T.ObjectID = I.ObjectID
			outer apply [dbo].[GetAssetTypeUrlById](T.ID) TUrl            		
			left outer join reporting.Global_Resource R on R.ResourceID = I.CreatedBy
			left outer join Comment C on C.ID = I.CommentID
            left join workflow.ItemAssignment IA on IA.ItemID = wi.ID and IA.ResourceObject = 'Resource'
            outer apply (select  top 1
                f.FormattedValue as [Value]
                from
                fieldtype ft
                inner
                join field f on (ft.id = f.fieldtypeid and f.[objecttype] = 'Issue' and f.objectid = I.ID
                and ft.FriendlyName = 'Description')) as DD
order by wi.StartedOn desc";

            var list = Company.Query<dynamic>(sql, new { id = objectid, obj = objecttype });

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }


        /// <summary>
        /// Gets the status of a given workflow, containing all steps executed as well as assignments.
        /// </summary>
        /// <param name="id">The ID of the workflow record to retrieve status for.</param>
        /// <param name="version">Version</param>
        /// <param name="uid"></param>
        /// <returns></returns>
        [Route("diagram/{id:int}/{uid:Guid}")]
        public WorkflowDiagramModel GetWorkflowDiagram(int id, Guid? uid,int? version = null)
        {
            if (id==0 && uid.HasValue && uid.Value != Guid.Empty)
                id = Company.Filter<core.entities.Workflow.Type>(i => i.UID == uid.Value).SingleOrDefault().ID;

            var nodes = Company.Query<WorkflowDiagramNode>(QueryConstants.WorkflowDiagramNodes, new { id, version }).ToList();
            var links = Company.Query<WorkflowDiagramLink>(QueryConstants.WorkflowDiagramLinks, new { id, version }).ToList();
            var type = Company.WorkflowTypes.Find(id);
            var @event = Company.WorkflowEventRegistrations.Single(e => e.TypeID == id);


            var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First();
            var publishedVersion = Company.WorkflowVersions.Find(type.PublishedVersionID);
            var model = new WorkflowDiagramModel()
            {
                Event = @event,
                Nodes = nodes
            };

            @event.ConditionObject = XmlToDynamic(GetConditionLabels(@event.Condition));
            List<FieldType> fieldTypes = GetFieldsForDiagramModel(model);
            nodes.ForEach(n =>
            {
                n.SettingsObject = XmlToDynamic(this.DeFormatMessageBodyTemplate(@event.Object, fieldTypes, n.Settings), false);
                n.FieldsObject = XmlToDynamic(this.DeFormatFormDescription(@event.Object, fieldTypes, n.Fields));
            });

            links.ForEach(l =>
            {
                l.ConditionObject = XmlToDynamic(GetConditionLabels(l.Condition));
                l.SettingsObject = XmlToDynamic(l.Settings);
            });

            @event.SettingsObject = XmlToDynamic(@event.Settings, false);
            
            //Augment existing schedule with only interval with defaults for days and type
            if (@event.SettingsObject.Settings != null &&
                @event.SettingsObject.Settings.ScheduleInterval != null &&
                @event.SettingsObject.Settings.ScheduleType == null)
            {
                @event.SettingsObject.Settings.ScheduleType = "d";
                @event.SettingsObject.Settings.ScheduleDays = 127;
            }

            return new WorkflowDiagramModel
            {
                Nodes = nodes,
                Links = links,
                Type = type,
                Event = @event,
                CurrentVersion = currentVersion,
                PublishedVersion = publishedVersion
            };
        }

        [HttpPost, Route("ReassignWorkflowResource/{itemStepId:int}/{resourceId:int}/{clearAssignments:bool}")]
        public async Task<HttpResponseMessage> ReassignWorkflowResource(int itemStepId, int resourceId, bool clearAssignments)
        {
            try
            {
                var itemStep = Company.WorkflowItemSteps.FirstOrDefault(x => x.ID == itemStepId);
                if (itemStep == null)
                    throw new Exception(WorkflowApiMessages.InvalidWorkflowStepID);
                else
                {
                    var resource = Company.GlobalReportingResources.Where(x => x.ResourceID == resourceId).ToList().FirstOrDefault();
                    await Company.BulkWorkflowFormReassign(new List<WorkflowItemStep> { itemStep }, resource, Company.CurrentResourceID, true, clearAssignments);
                }
                return Request.CreateResponse(HttpStatusCode.Accepted, -1);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost, Route("ReassignWorkflowObject/{itemId:int}/{workflowId:int}/{objectId:int}/{objectType}/{itemStepId:int}")]
        public HttpResponseMessage ReassignWorkflowObject(int itemId, int workflowId, int objectId, string objectType, int itemStepId, int? resourceId)
        {
            try
            {
                //look up change event registration
                var reg = Company.WorkflowEventRegistrations.Where(x => x.TypeID == workflowId).FirstOrDefault();

                if (reg == null) return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.InvalidWorkflowRegistration);

                //add new event for the requested object and change type                
                var issue = Company.AssignActivityWorkflowToNewObject(reg, itemId, workflowId, objectId, objectType);

                //terminate current workflow

                var workflowItem = Company.WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();

                if (workflowItem == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.InvalidWorkflowID);
                }

                workflowItem.CompletedBy = Company.CurrentResourceID;
                workflowItem.CompletedOn = DateTime.UtcNow;


                //mark the form as completed as well since it was reassisnged
                var workflowItemStep = Company.WorkflowItemSteps.Where(x => x.ID == itemStepId).FirstOrDefault();

                if (workflowItemStep == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.InvalidWorkflowStepID);
                }

                var isResourceReassignment = objectType.ToLower() == "resource";

                var fieldElement = XElement.Parse(workflowItemStep.Fields);
                var reassigned = new XElement("Reassigned");
                reassigned.Add(new XAttribute("reassignType", (isResourceReassignment ? "Resource" : "Object")));
                if (isResourceReassignment)
                {
                    reassigned.Add(new XAttribute("toResourceId", objectId));
                    reassigned.Add(new XAttribute("fromResourceId", resourceId ?? Company.CurrentResourceID));
                }
                else
                {
                    reassigned.Add(new XAttribute("objectId", objectId));
                    reassigned.Add(new XAttribute("objectType", objectType));
                }

                reassigned.Add(new XAttribute("byResourceId", Company.CurrentResourceID.ToString()));
                reassigned.Add(new XAttribute("reassignOn", DateTime.UtcNow));
                reassigned.Add(new XAttribute("newIssueId", issue.ID.ToString()));

                fieldElement.Add(reassigned);
                workflowItemStep.Fields = fieldElement.ToString();

                workflowItemStep.CompletedBy = Company.CurrentResourceID;
                workflowItemStep.CompletedOn = DateTime.UtcNow;

                Company.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Accepted, -1);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost, Route("SubmitWorkflowForm/{itemId:int}/{itemStepId:int}")]
        public async Task<HttpResponseMessage> SubmitWorkflowForm(int itemId, int itemStepId, List<WorkflowFormModelField> model)
        {
            try
            {
                int numberOfResponses = 1;
                int totalResources = 0;
                var item = Company.WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();
                var itemStepsModel = Company.WorkflowItemSteps.Where(x => x.ID == itemStepId).FirstOrDefault();

                var versionStep = Company.WorkflowVersionSteps.Where(x => x.ID == itemStepsModel.StepID).FirstOrDefault();

                if (itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, WorkflowApiMessages.ItemStepNotFound);
                }

                if (string.IsNullOrEmpty(itemStepsModel.Settings))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.InvalidFormSetting);

                var formSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(versionStep.Settings));
                var isCompleted = false;

                StringBuilder sb = new StringBuilder();

                var root = XElement.Parse(itemStepsModel.Fields);

                //increment the number of responses attribute

                if (root.Attribute("NumberOfResponses") != null)
                {
                    int.TryParse((string)root.Attribute("NumberOfResponses"), out numberOfResponses);
                    root.Attribute("NumberOfResponses").SetValue(++numberOfResponses);
                }
                else
                {
                    root.Add(new XAttribute("NumberOfResponses", numberOfResponses));
                }

                if (root.Attribute("TotalResources") != null)
                {
                    int.TryParse((string)root.Attribute("TotalResources"), out totalResources);
                }

                var newForm = new XElement("form", new XAttribute("ResourceID", Company.CurrentResourceID));

                foreach (var field in model)
                {
                    var val = field.Value != null ? field.Value.ToString() : "";
                    var displayVal = val;
                    if (field.FieldType == WorkflowFormModelFieldType.boolean)
                    {
                        val = (val ?? "").ToUpper() == "TRUE" ? "TRUE" : "FALSE";
                    }
                    else if (field.FieldType == WorkflowFormModelFieldType.list)
                    {
                        var fieldTypeId = int.Parse(field.ReferenceFieldID);
                        var fieldType = Company.FieldTypes.Where(x => x.ID == fieldTypeId).FirstOrDefault();
                        int intVal = 0;

                        if (field.AllowMultipleValues)
                        {
                            var values = val.Split(',');
                            displayVal = "";
                            foreach (var v in values)
                            {
                                if (fieldType != null && int.TryParse(v, out intVal))
                                {
                                    var lookup = Company.FieldLookupValues.Where(x => x.LookupObjectID == fieldType.LookupObjectID && x.Value == intVal && x.LookupObjectType == fieldType.LookupObjectType).FirstOrDefault();

                                    if (!string.IsNullOrEmpty(displayVal)) displayVal += ",";

                                    if (lookup != null)
                                    {
                                        displayVal += lookup.Text;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (fieldType != null && int.TryParse(val, out intVal))
                            {

                                var lookup = Company.FieldLookupValues.Where(x => x.LookupObjectID == fieldType.LookupObjectID && x.Value == intVal && x.LookupObjectType == fieldType.LookupObjectType).FirstOrDefault();

                                if (lookup != null)
                                {
                                    displayVal = lookup.Text;
                                }
                            }
                        }
                    }
                    else if (field.FieldType == WorkflowFormModelFieldType.html)
                    {
                        var sanitizer = new Ganss.XSS.HtmlSanitizer();
                        sanitizer.AllowedSchemes.Add("data");
                        val = sanitizer.Sanitize(val);
                    }


                    newForm.Add(new XElement("field",
                            new XAttribute("id", field.ID),
                            new XAttribute("label", field.Label),
                            new XAttribute("value", val),
                            new XAttribute("displayvalue", displayVal),
                            new XAttribute("fieldtype", field.FieldType.ToString().ToLower()))
                        );
                }

                root.Add(newForm);

                if (itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.ItemStepUnableFound);
                }

                // check the settings for the form.  If the form is set to first response then we mark the step as complete and fire off its transitions
                switch (formSettings.ResponseType)
                {
                    case FormResponseType.FirstResponse:
                        isCompleted = true;
                        //complete step and go to transitions                        
                        break;
                    case FormResponseType.All:
                        isCompleted = numberOfResponses >= totalResources;
                        //check that the number of users requested to fill out the form matches the number of responses recieved.
                        break;
                    case FormResponseType.Majority:
                        //wait for all users to complete so we can determine what the majority says.
                        isCompleted = numberOfResponses >= (totalResources / 2) + 1;
                        //isCompleted = numberOfResponses >= totalResources;
                        break;
                }

                itemStepsModel.Fields = root.ToString(SaveOptions.None);

                if (isCompleted)
                {
                    itemStepsModel.CompletedOn = DateTime.UtcNow;
                    itemStepsModel.CompletedBy = Company.CurrentResourceID;
                }

                Company.Entry(itemStepsModel).State = System.Data.Entity.EntityState.Modified;

                //remove any assignment records in the workflow item assignment table so this item doesnt appear assigned to this user anymore
                var assignment = Company.WorkflowItemAssignments.Where(x => x.ItemID == itemId && x.ResourceObject == "Resource" && x.ResourceObjectID == Company.CurrentResourceID && x.ItemStepID == itemStepsModel.ID).FirstOrDefault();

                if (assignment != null)
                {
                    Company.WorkflowItemAssignments.Remove(assignment);
                }

                Company.SaveChanges();

                var @object = (SystemObjects)Enum.Parse(typeof(SystemObjects), item.Object);

                var obj = Company.GetObjectDetail(item.Object, item.ObjectID);

                var type = SystemObjects.IssueType;

                if (obj != null) type = (SystemObjects)Enum.Parse(typeof(SystemObjects), obj.Type);


                if (isCompleted)
                {
                    //clear other assignments
                    Company.CompleteItemStepAssignments(itemStepId);

                    SendEvent("Workflow Form Completed", new Dictionary<string, string> { { "WorkflowItemID", "itemId" }, { "ResourceID", Company.CurrentResourceID.ToString() } });
                    int transitionsCount = await Company.MarkStepAsCompleteAndContinue(itemStepsModel, itemId, new core.queue.EventObjectInfo { Object = @object, ObjectID = item.ObjectID, ObjectTypeID = (obj != null ? obj.TypeID : -1), ObjectType = type });

                    if (transitionsCount == 0)
                    {
                        //log that a form was submited that had 0 transitions
                        SendEvent("Form completed with 0 transitions");
                    }


                    Company.SaveChanges();
                }

                return Request.CreateResponse(HttpStatusCode.Accepted, itemStepsModel);
            }
            catch (Exception ex)
            {
                SendException(ex, new Dictionary<string, string> { { "WorkflowItemID", "itemId" } });

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost, Route("SubmitWorkflowForm/bulk")]
        public async Task<HttpResponseMessage> SubmitBulkWorkflowForm(BulkWorkflowFormModel model)
        {

            //model validation

            if (model == null || model.ItemStepIDs == null || model.ItemStepIDs.Count < 1)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);

            var itemSteps = Company.WorkflowItemSteps.Where(x => model.ItemStepIDs.Contains(x.ID)).Include(x => x.Item).Include(x => x.Step).ToList();

            if (itemSteps == null || itemSteps.Count < 1)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.NoValidItemStep);

            var stepID = itemSteps.First().StepID;

            if (itemSteps.Any(i => i.StepID != stepID))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, WorkflowApiMessages.MultiVersionFound);

            var versionStep = Company.WorkflowVersionSteps.Where(x => x.ID == stepID).Include(x => x.Version).FirstOrDefault();

            if (versionStep == null)
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, WorkflowApiMessages.InvalidSpecificID);

            if (model.Fields == null)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, WorkflowApiMessages.InvalidModelNoFieldPassed);


            try
            {
                var omittedCount = 0;
                var validItemSteps = new List<WorkflowItemStep>();
                foreach (var i in itemSteps)
                {


                    var formResults = XElement.Parse(i.Fields);
                    bool isCompletedByCurrentUser = false;
                    bool isDeleted = false;
                    ObjectDetail details = null;

                    switch (i.Item.Object)
                    {
                        case "Issue":
                            var issue = Company.Issues.Where(x => x.ID == i.Item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                            if (issue != null)
                            {
                                details = new ObjectDetail
                                {
                                    Type = "Action",
                                    Name = issue.Object,
                                    TypeName = issue.IssueType.Name
                                };
                            }
                            break;
                        default:
                            details = Company.GetObjectDetail(i.Item.Object, i.Item.ObjectID);
                            break;
                    }

                    if (details == null)
                    {
                        isDeleted = true;
                        omittedCount++;
                    }


                    foreach (var form in formResults.Elements("form"))
                    {
                        int completedById = 0;
                        int.TryParse((string)form.Attribute("ResourceID") ?? "", out completedById);

                        if (completedById == Company.CurrentResourceID && !isCompletedByCurrentUser)
                        {
                            isCompletedByCurrentUser = true;
                            continue;
                        }
                    }

                    // check if the user has access
                    var IsUserAllowedToComplete = Company.WorkflowItemAssignments.Where(x => x.ItemStepID == i.ID && x.ResourceObjectID == Company.CurrentResourceID).Any();

                    // if user does not have access or item has been deleted, don't add it to the list of valid item steps
                    if (IsUserAllowedToComplete && !isCompletedByCurrentUser && !i.CompletedOn.HasValue && !isDeleted)
                        validItemSteps.Add(i);

                }

                //submit the valid item steps
                foreach (var i in validItemSteps)
                    await SubmitWorkflowForm((int)i.ItemID, (int)i.ID, model.Fields);


                return Request.CreateResponse(HttpStatusCode.OK, new { success = true, totalCount = validItemSteps.Count(), omittedCount });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [Route("form/{typeID:int}/{itemStepID:int}"), HttpGet]
        public async Task<HttpResponseMessage> GetWorkflowForm(int typeID, int itemStepID)
        {
            var itemStep = Company.WorkflowItemSteps.Where(x => x.ID == itemStepID).Include(x => x.Item).Include(x => x.Step).FirstOrDefault();


            if (itemStep == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.WorkflowItemDeleted);
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
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, WorkflowApiMessages.WofkFlowXMLNull);
            }

            var desc = (string)XElement.Parse(xml).Element("form").Attribute("description");
            var title = (string)XElement.Parse(xml).Element("form").Attribute("title");
            bool.TryParse((string)XElement.Parse(xml).Element("form").Attribute("allowReassignResource"), out bool allowReassignResource);
            bool.TryParse((string)XElement.Parse(xml).Element("form").Attribute("allowReassignObject"), out bool allowReassignObject);

            if (string.IsNullOrEmpty(xml))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.VersionStepIDNotFound);

            List<WorkflowFormModelField> properties = (
                                 from s in XElement.Parse(xml).Element("form").Elements()
                                 select new WorkflowFormModelField
                                 {
                                     Value = (string)s.Attribute("value"),
                                     ID = (string)s.Attribute("id"),
                                     Label = (string)s.Attribute("label"),
                                     ReferenceFieldID = (string)s.Attribute("referenceFieldId"),
                                     Required = s.Attribute("required") == null ? false : (bool)s.Attribute("required"),
                                     IntersectTypeID = int.Parse((string)s.Attribute("intersectTypeId") ?? "0"),
                                     FieldType = (WorkflowFormModelFieldType)Enum.Parse(typeof(WorkflowFormModelFieldType), (string)s.Attribute("type"))
                                 }
                                 ).ToList();



            ObjectDetail details = null;
            ObjectDetail issueItemDetails = null;
            var issueTypeName = "";
            var issueObjectType = "";
            var typeName = "";
            var formError = false;

            foreach (var item in properties)
            {
                int fieldId = 0;

                if (item.FieldType == WorkflowFormModelFieldType.relationshipType)
                {
                    if (item.IntersectTypeID <= 0)
                    {
                        throw new Exception(WorkflowApiMessages.RelatioshipInvalid);
                    }
                    //load the possible options for this relationship type into values array
                    var intersectType = Company.IntersectTypes.Where(x => x.ID == item.IntersectTypeID).FirstOrDefault();

                    if (intersectType == null)
                    {
                        formError = true;
                        continue;
                    }

                    var reg = Company.WorkflowEventRegistrations.Where(x => x.TypeID == typeID).FirstOrDefault();

                    if (reg == null) 
                    {
                        throw new Exception(WorkflowApiMessages.RelationNotFoundRegistration);
                    }

                    var obj = reg.Object;
                    var objId = reg.ObjectID;

                    if (reg.Object == "IssueType")
                    {
                        var issue = Company.Issues.FirstOrDefault(i => i.ID == itemStep.Item.ObjectID);
                        if (issue == null)
                        {
                            formError = true;
                            continue;
                        }

                        obj = issue.ObjectType;
                        objId = issue.ObjectTypeID;
                    }

                    var itemSql = "select A.DisplayValue as [Text], A.Object + '|' + cast(A.ObjectID as varchar) as [Value] from AssetDetail A where A.Type = @objectType and A.TypeID = @objectTypeId order by 1";

                    item.Values = new List<System.Web.Mvc.SelectListItem>();

                    if (obj == intersectType.Subject && objId == intersectType.SubjectID)
                    {
                        // load the object items into the values array                        
                        item.AllowMultipleValues = !(intersectType.ObjectCardinality == core.enums.Cardinality.One);

                        item.Values.AddRange(
                            Company.Query<System.Web.Mvc.SelectListItem>(itemSql, new { objectType = intersectType.Object, objectTypeId = intersectType.ObjectID })
                        );
                    }
                    else
                    {
                        item.AllowMultipleValues = !(intersectType.SubjectCardinality == core.enums.Cardinality.One);
                        // load the subject items into the value array
                        item.Values.AddRange(
                            Company.Query<System.Web.Mvc.SelectListItem>(itemSql, new { objectType = intersectType.Subject, objectTypeId = intersectType.SubjectID })
                        );
                    }
                }

                if (string.IsNullOrEmpty(item.ReferenceFieldID) || !int.TryParse(item.ReferenceFieldID, out fieldId) || item.FieldType != WorkflowFormModelFieldType.list) continue;

                //load the field type
                var fieldType = Company.FieldTypes.Where(x => x.ID == fieldId).FirstOrDefault();

                if (fieldType == null) continue;

                //get the possible values for this field
                if (!string.IsNullOrEmpty(fieldType.LookupObjectType))
                {
                    try
                    {
                        item.AllowMultipleValues = fieldType.AllowMultipleValues;
                        item.Values = new List<System.Web.Mvc.SelectListItem>();

                        item.Values.AddRange(
                            Company.Filter<FieldLookupValue>(o => o.FieldTypeID == fieldType.ID && o.LookupObjectType == fieldType.LookupObjectType && o.LookupObjectID == fieldType.LookupObjectID.Value)
                                .OrderBy(o => o.Text)
                                .Select(i => new System.Web.Mvc.SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                .ToList()
                        );
                    }
                    catch { }
                }
            }

            switch (itemStep.Item.Object)
            {
                case "Issue":
                    var issue = Company.Issues.Where(x => x.ID == itemStep.Item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                    if (issue != null)
                    {
                        var comment = Company.Comments.Where(x => x.ID == issue.CommentID).FirstOrDefault();
                        details = new ObjectDetail
                        {
                            Type = "Action",
                            Name = comment != null ? comment.Body : "",
                            TypeName = issue.IssueType.Name
                        };

                        if (issue.IssueType != null)
                            issueTypeName = issue.IssueType.Name;
                        issueItemDetails = Company.GetObjectDetail(issue.Object, issue.ObjectID);
                        issueObjectType = issue.Object;
                        typeName = details.TypeName;
                    }
                    break;
                default:
                    details = Company.GetObjectDetail(itemStep.Item.Object, itemStep.Item.ObjectID);
                    typeName = (details != null ? details.TypeName : "");
                    break;
            }

            if (itemStep.Item.Object == "Intersect" && string.IsNullOrEmpty(typeName))
                typeName = "Relationship";

            var formSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(itemStep.Step.Settings));

            //check if the current user already completed the form
            var formResults = XElement.Parse(itemStep.Fields);
            bool isCompletedByCurrentUser = false;

            foreach (var form in formResults.Elements("form"))
            {
                int completedById = 0;
                int.TryParse((string)form.Attribute("ResourceID") ?? "", out completedById);

                if (completedById == Company.CurrentResourceID && !isCompletedByCurrentUser)
                {
                    isCompletedByCurrentUser = true;
                    continue;
                }
            }

            // check if the user has access
            var IsUserAllowedToComplete = Company.WorkflowItemAssignments.Where(x => x.ItemStepID == itemStep.ID && x.ResourceObjectID == Company.CurrentResourceID).Any();
            //See if there are more than one user and the response type is first response if so we can give option to reassign to a sigle user and clear other assignments.
          
            
            var itemFields = (WorkflowItemStepDetail.FieldsModel)new XmlSerializer(typeof(WorkflowItemStepDetail.FieldsModel)).Deserialize(new StringReader(itemStep.Fields));
            var assignments = Company.WorkflowItemAssignments.Where(x => x.ItemStepID == itemStep.ID).Count();
            var IsClearAssignementsAllowed = (assignments > 1)
                && (formSettings.ResponseType == FormResponseType.FirstResponse);



            //replace any tokens in the description            
            desc = await Company.ProcessMessageTokens(desc, itemStep.Item.ObjectID, (SystemObjects)Enum.Parse(typeof(SystemObjects), itemStep.Item.Object), Company.CurrentCompanyDomain, itemStep, true, false, false);

            //parse the xml to get the form info

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, new
            {
                Fields = properties,
                Title = title ?? "",
                Description = desc ?? "",
                IsCompleted = itemStep.CompletedOn.HasValue || isCompletedByCurrentUser,
                IsItemDeleted = details == null,
                IsFormInvalid = formError,
                ObjectName = details == null ? "(unknown)" : details.Name,
                ObjectType = itemStep.Item.Object,
                ObjectID = itemStep.Item.ObjectID,
                ObjectTypeID = details?.TypeID ?? 0,
                TypeName = typeName,
                IsUserAllowedToComplete = IsUserAllowedToComplete,
                IssueObject = issueObjectType,
                IssueObjectID = issueItemDetails != null ? issueItemDetails.ID : 0,
                IssueObjectName = issueItemDetails != null ? issueItemDetails.Name : "",
                IssueTypeName = issueTypeName,
                AllowReassignObject = allowReassignObject,
                AllowReassignResource = allowReassignResource,
                IsClearAssignementsAllowed
            });
        }

        [Route("form/bulk"), HttpPost]
        public async Task<HttpResponseMessage> GetBulkWorkflowForm(BulkWorkflowFormModel model)
        {
            try
            {
                //model validation

                if (model == null || model.ItemStepIDs == null || model.ItemStepIDs.Count < 1)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);

                var itemStepID = model.ItemStepIDs.First();
                var itemStep = Company.WorkflowItemSteps.Where(x => x.ID == itemStepID).Include(x => x.Item).Include(x => x.Step).FirstOrDefault();

                if (itemStep == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.NoValidItemStep);

                var stepID = itemStep.StepID;

                var versionStep = Company.WorkflowVersionSteps.Where(x => x.ID == stepID).Include(x => x.Version).FirstOrDefault();

                if (versionStep == null)
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, WorkflowApiMessages.InvalidSpecificID);


                var typeID = versionStep.Version.TypeID;
                var type = Company.GetById<core.entities.Workflow.Type>(typeID);

                string sql = @"
                    SELECT vs.[Fields]      
                      FROM 
	                    [workflow].[VersionStep] vs
                        inner join [workflow].[itemstep] wis on(vs.id = wis.stepid)
                     where wis.stepid = @id
                ";

                var xml = (await Company.QueryAsync<string>(sql, new { id = stepID })).FirstOrDefault();

                if (string.IsNullOrEmpty(xml))
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, WorkflowApiMessages.WofkFlowXMLNull);

                var desc = (string)XElement.Parse(xml).Element("form").Attribute("description");
                var title = (string)XElement.Parse(xml).Element("form").Attribute("title");

                if (string.IsNullOrEmpty(xml))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.VersionStepIDNotFound);

                List<WorkflowFormModelField> properties = (
                                     from s in XElement.Parse(xml).Element("form").Elements()
                                     select new WorkflowFormModelField {
                                         Value = (string)s.Attribute("value"),
                                         ID = (string)s.Attribute("id"),
                                         Label = (string)s.Attribute("label"),
                                         ReferenceFieldID = (string)s.Attribute("referenceFieldId"),
                                         Required = s.Attribute("required") == null ? false : (bool)s.Attribute("required"),
                                         IntersectTypeID = int.Parse((string)s.Attribute("intersectTypeId") ?? "0"),
                                         FieldType = (WorkflowFormModelFieldType)Enum.Parse(typeof(WorkflowFormModelFieldType), (string)s.Attribute("type"))
                                     }
                                     ).ToList();


                ObjectDetail details = null;
                ObjectDetail issueItemDetails = null;
                var issueTypeName = "";
                var issueObjectType = "";

                foreach (var item in properties)
                {
                    int fieldId = 0;

                    if (item.FieldType == WorkflowFormModelFieldType.relationshipType)
                    {
                        if (item.IntersectTypeID <= 0)
                        {
                            throw new Exception(WorkflowApiMessages.RelatioshipInvalid);
                        }
                        //load the possible options for this relationship type into values array
                        var intersectType = Company.IntersectTypes.Where(x => x.ID == item.IntersectTypeID).FirstOrDefault();

                        if (intersectType == null) 
                        {
                            throw new Exception(WorkflowApiMessages.RelationNotFoundIntersectType);
                        }

                        var reg = Company.WorkflowEventRegistrations.Where(x => x.TypeID == typeID).FirstOrDefault();

                        if (reg == null)
                        {
                            throw new Exception(WorkflowApiMessages.RelationNotFoundRegistration);
                        }

                        var obj = reg.Object;
                        var objId = reg.ObjectID;

                        if (reg.Object == "IssueType")
                        {
                            var issue = Company.Issues.FirstOrDefault(i => i.ID == itemStep.Item.ObjectID);
                            if (issue == null) 
                            {
                                throw new Exception(WorkflowApiMessages.RelationNotFoundIssueObject);
                            }

                            obj = issue.ObjectType;
                            objId = issue.ObjectTypeID;
                        }

                        var itemSql = "select i.Name as Text, i.Object + '|' + cast(i.ObjectID as varchar) as Value from AssetType i where i.object = @objectType and i.objectid = @objectTypeId order by 1";

                        item.Values = new List<System.Web.Mvc.SelectListItem>();

                        if (obj == intersectType.Subject && objId == intersectType.SubjectID)
                        {
                            // load the object items into the values array                        
                            item.AllowMultipleValues = !(intersectType.ObjectCardinality == core.enums.Cardinality.One);

                            item.Values.AddRange(
                                Company.Query<System.Web.Mvc.SelectListItem>(itemSql, new { objectType = intersectType.Object, objectTypeId = intersectType.ObjectID })
                            );
                        }
                        else
                        {
                            item.AllowMultipleValues = !(intersectType.SubjectCardinality == core.enums.Cardinality.One);
                            // load the subject items into the value array
                            item.Values.AddRange(
                                Company.Query<System.Web.Mvc.SelectListItem>(itemSql, new { objectType = intersectType.Subject, objectTypeId = intersectType.SubjectID })
                            );
                        }
                    }

                    if (string.IsNullOrEmpty(item.ReferenceFieldID) || !int.TryParse(item.ReferenceFieldID, out fieldId) || item.FieldType != WorkflowFormModelFieldType.list) continue;

                    //load the field type
                    var fieldType = Company.FieldTypes.Where(x => x.ID == fieldId).FirstOrDefault();

                    if (fieldType == null) continue;

                    //get the possible values for this field
                    if (!string.IsNullOrEmpty(fieldType.LookupObjectType))
                    {
                        try
                        {
                            item.AllowMultipleValues = fieldType.AllowMultipleValues;
                            item.Values = new List<System.Web.Mvc.SelectListItem>();

                            item.Values.AddRange(
                                Company.Filter<FieldLookupValue>(o => o.FieldTypeID == fieldType.ID && o.LookupObjectType == fieldType.LookupObjectType && o.LookupObjectID == fieldType.LookupObjectID.Value)
                                    .OrderBy(o => o.Text)
                                    .Select(i => new System.Web.Mvc.SelectListItem { Text = i.Text, Value = i.Value.ToString() })
                                    .ToList()
                            );
                        }
                        catch { }
                    }
                }

                var formSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(versionStep.Settings));

                var typeName = "";
                var objectName = "";

                switch (itemStep.Item.Object)
                {
                    case "Issue":
                        var issue = Company.Issues.Where(x => x.ID == itemStep.Item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                        if (issue != null)
                        {
                            details = new ObjectDetail
                            {
                                Type = "Action",
                                Name = issue.Object,
                                TypeName = issue.IssueType.Name
                            };
                        }
                        break;
                    default:
                        details = Company.GetObjectDetail(itemStep.Item.Object, itemStep.Item.ObjectID);
                        break;
                }

                if (details != null)
                {
                    typeName = details.TypeName;
                    objectName = details.Type == "Action" ? details.Name : itemStep.Item.Object;
                }



                return Request.CreateResponse<dynamic>(HttpStatusCode.OK, new
                {
                    WorkflowName = type?.Name ?? "",
                    TypeName = typeName,
                    versionStep.Version.Version,
                    ObjectName = string.IsNullOrEmpty(objectName) ? "(unknown)" : objectName,

                    Fields = properties,
                    Title = title ?? "",
                    Description = desc ?? "",
                    IssueObject = issueObjectType,
                    IssueObjectID = issueItemDetails != null ? issueItemDetails.ID : 0,
                    IssueObjectName = issueItemDetails != null ? issueItemDetails.Name : "",
                    IssueTypeName = issueTypeName,
                    model.ItemStepIDs,
                    OmittedCount = 0
                });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete, Route("deleteItems")]
        public HttpResponseMessage DeleteWorkfowItems([FromBody] int[] items)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }
            if (items.Length > 0)
            {
                string inclause = string.Join(",", items.Select((s, i) => "@p" + i.ToString()).ToArray());
                var parameters = new DynamicParameters();
                for (var i = 0; i < items.Length; i++)
                {
                    parameters.Add("p" + i.ToString(), items[i]);
                }
                string sql = "";
                sql = @"DELETE FROM [workflow].[ItemStepTransition] WHERE [FromItemStepID] IN (SELECT [ID] FROM [workflow].[ItemStep] WHERE [ItemID] IN (" + inclause + "))";
                int rows = Company.Database.Connection.Execute(sql, parameters);
                sql = @"DELETE FROM [workflow].[ItemStep] WHERE [ItemID] IN  (" + inclause + ")";
                rows = Company.Database.Connection.Execute(sql, parameters);
                sql = @"DELETE FROM [workflow].[Item] WHERE [ID] IN  (" + inclause + ")";
                rows = Company.Database.Connection.Execute(sql, parameters);
            }
            return Request.CreateResponse(HttpStatusCode.OK, items.Length);
        }

        [Route("activitytypes"), HttpGet]
        public List<core.enums.Workflow.ActivityTypeInfo> GetActivityTypes()
        {
            var items = d360.core.enums.Workflow.WorkflowActivityType.EmailNotification.GetList().ToList();

            if (Company.WorkflowTaskProcedures.Count() <=0)
            {
                var itemvalue =  items.FirstOrDefault(i => i.Name == "Procedure");
                itemvalue.IsShow = false;
            }

            return items;
        }

        [Route("emailtaskrecipienttypes"), HttpGet]
        public List<EmailTaskRecipientTypeInfo> GetEmailTaskRecipientTypes()
        {
            return EmailTaskRecipientType.None.GetList().ToList();
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

        [Route("admintypes"), HttpGet]
        public HttpResponseMessage GetWorkflowAdminTypes()
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
            }

            var types = Company.Query<dynamic>(QueryConstants.WorkflowList).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("types"), HttpGet]
        public HttpResponseMessage GetWorkflowTypes()
        {
            var types = Company.Query<dynamic>(QueryConstants.WorkflowList).ToList();

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
                    ,v.ID as VersionID
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

        [Route("items/{versionId:int}"), HttpGet]
        public HttpResponseMessage GetItemsForWorkflow(int versionId)
        {
            string sql = @"select
	                        i.[object] as 'Object'
	                        ,i.objectid as 'ObjectId'
	                        ,i.updatedon as 'UpdatedOn'
	                        ,i.completedon as 'CompletedOn'
                            ,i.numberofevents as 'NumberOfEvents'
	                        ,coalesce(od.DisplayValue,IName.Name) as 'Name'
                            ,AUrl.[Url] as 'Url'
                            ,i.id as 'ItemID'
                          from
	                        [workflow].[version] v
	                        inner join [workflow].item i on v.id = i.versionid
	                        left join AssetDetail od on i.objectid = od.objectid and i.[object] = od.[object] 
							outer apply [dbo].[GetAssetUrlById](od.ID) AUrl
							left join [Intersect] IT on i.Object = 'Intersect' and I.ObjectID = IT.ID
							outer apply dbo.GetIntersectNames(IT.ID) IName	                         
                          where 
	                        coalesce(od.ID, it.ID) is not null and v.id = @id
            ";

            var types = Company.Query<dynamic>(sql, new { id = versionId }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("item/detail/{itemId:int}"), HttpGet]
        public HttpResponseMessage GetItemDetail(int itemId)
        {
            var item = Company.WorkflowItems.Include(x => x.Version).Where(x => x.ID == itemId).FirstOrDefault();

            if (item == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, WorkflowApiMessages.WorkflowInstanceNotFound);

            // get the itemsteps for this workflow instance
            var sql = @"
                select 
	                si.* 
                from 
	                workflow.itemstep si 
	                outer apply (
		                select case when vs.Settings.value('/settings[1]/WaitForAllTransitions[1]','varchar(max)') = 'true' then
			                1
		                else
			                0
		                end as [value]
		                from workflow.versionstep vs where vs.id = si.stepid
	                ) waitForAll
                where 
	                si.itemid = @itemId
	                and (waitForAll.[value] = 0 or (waitForAll.[value] = 1 and si.id = (select max(id) from workflow.itemstep where itemid = si.itemid and stepid = si.stepid)))
                order by si.id";

            var itemSteps = Company.Query<WorkflowItemStep>(sql, new { itemId });

            var stepIDs = itemSteps.Select(y => y.StepID).ToArray();
            var steps = Company.WorkflowVersionSteps.Where(x => stepIDs.Contains(x.ID)).ToList();
            var workflow = Company.WorkflowTypes.Where(x => x.ID == item.Version.TypeID).FirstOrDefault();

            ObjectDetail objectDetails = null;
            var actionAsset = new Asset();
            switch (item.Object)
            {
                case "Issue":
                    var issue = Company.Issues.Where(x => x.ID == item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                    if (issue != null)
                    {
                        var comment = Company.Comments.Where(x => x.ID == issue.CommentID).FirstOrDefault();
                        actionAsset = Company.Assets.FirstOrDefault(x => x.Object == issue.Object && x.ObjectID == issue.ObjectID);

                        objectDetails = new ObjectDetail
                        {
                            Type = "Action",
                            Name = comment != null ? comment.Body : "",
                            TypeName = issue.IssueType.Name
                        };
                    }
                    break;
                default:
                    objectDetails = Company.GetObjectDetail(item.Object, item.ObjectID);
                    break;
            }

            return Request.CreateResponse(HttpStatusCode.OK,
                new
                {
                    Item = item,
                    Workflow = workflow,
                    ItemSteps = itemSteps,
                    Steps = steps,
                    ObjectDetails = objectDetails,
                    ActionAsset = actionAsset
                });
        }

        [Route("item/details/{workflowId:int}/{itemId:int}"), HttpGet]
        public HttpResponseMessage GetItemDetailsForWorkflow(int workflowId, int itemId)
        {
            string sql = @"		select
			                        vs.name as 'Name',
			                        istep.startedOn as 'StartedOn',
                                    R.FirstName + ' ' + R.LastName as StartedBy, 
			                        istep.completedon as 'CompletedOn',
                                    Rc.FirstName + ' ' + Rc.LastName as CompletedBy,
                                    vs.ActivityType as ActivityType,
                                    vs.StepType as StepType,
									vsTo.name as ToStep,
                                    istep.settings as 'Settings'
                                from
			                        [workflow].item i
	                                inner join [workflow].itemstep istep on (i.id = istep.itemid)
                                    inner join [workflow].version v on (i.versionid = v.id)
			                        inner join [workflow].versionstep vs on (vs.id = istep.stepid)
                                    inner join [reporting].[Global_Resource] R on R.ResourceID = istep.startedby
                                    left join [reporting].[Global_Resource] Rc on Rc.ResourceID = istep.completedby
									left join [workflow].itemsteptransition itrans on (itrans.fromitemStepID = istep.id)
									left join [workflow].itemstep istepTo on (itrans.toitemstepid = istepTo.id)									                                    
			                        left join [workflow].versionstep vsTo on (vsTo.id = istepTo.stepid)
		                        where
			                        i.id = @itemId and v.typeid = @workflowId;
            ";

            var types = Company.Query<dynamic>(sql, new { workflowId = workflowId, itemId = itemId }).ToList();
            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("objecttypes"), HttpGet]
        public HttpResponseMessage GetObjectTypes(ChangeType changeType)
        {
            var types = Company.Query<dynamic>(QueryConstants.WorkflowObjectTypes).ToList();

            switch (changeType)
            {
                case ChangeType.Schedule:
                    types = types.Where(t => t.type == "ArtifactType" 
                    || t.type == "TaxonomyType" 
                    || t.type == "IssueType"
                    || t.type == "RuleType")
                        .OrderBy(t => t.name)
                        .ToList();
                    break;
                case ChangeType.ScoreUpdate:
                    types = types.Where(t => t.type == "ArtifactType" 
                    || t.type == "TaxonomyType" || t.type == "RuleType" 
                    || t.type == "PolicyType")
                        .OrderBy(t => t.name)
                        .ToList();
                    break;
                case ChangeType.RequestCertification:
                    types = types.Where(t => t.type != "IssueType" && t.type != "ReferenceItemType" && t.type != "IntersectType")
                        .OrderBy(t => t.name)
                        .ToList();
                    break;
                default:
                    types = types.OrderBy(t => t.name).ToList();
                    break;
            }


            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("scoretypes/{id:int}/{type}"), HttpGet]
        public HttpResponseMessage GetScoreTypes(string type, int id)
        {
            var results =  Company.Query<ScoreType>(@"select distinct ScoreType 
                from metrics.allocation A
                inner join AssetType T on T.[uid] = A.AssetTypeUid
                where a.[State] = 1 and T.[Object] = @type and T.ObjectID = @id", new { type, id })
                .Select(s => new { label = s.GetDisplayName(), value = (int)s })
                .ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("type/{id:int}/{uid:Guid}"), HttpGet]
        public HttpResponseMessage GetWorkflowType(int id,Guid? uid)
        {
            core.entities.Workflow.Type type;
            if (id==0 && uid.HasValue && uid.Value != Guid.Empty)
                type = Company.Filter<core.entities.Workflow.Type>(i => i.UID == uid.Value).SingleOrDefault();
            else
                type = Company.WorkflowTypes.Find(id);


            if (type == null || (type.State != core.enums.State.Active && type.State != core.enums.State.InActive))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.WorkflowtypeidNotFound, id.ToString()));


            var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First();
            var model = GetWorkflowDiagram(id, uid, currentVersion?.Version);

            model.Type = type;
            model.CurrentVersion = currentVersion;

            return Request.CreateResponse(HttpStatusCode.OK, model);
        }

        [Route("fieldtypes/{type}/{id:int}"), HttpGet]
        public HttpResponseMessage GetFieldTypes(int id, string type, bool allowHtml = false, string additionalFields = "")
        {

            var fields = this.getFieldTypes(id, type, allowHtml, additionalFields);
            return Request.CreateResponse(HttpStatusCode.OK, fields);
        }

        private List<FieldType> getFieldTypes(int id, string type, bool allowHtml = false, string additionalFields = "")
        {
            var fields = Company.FieldTypes.Where(f => f.Object == type && f.ObjectID == id).ToList();
            List<string> excludedTypes = DataType.Text.GetNonWorkflowConditionFields();
            if (!allowHtml)
                excludedTypes.Add("Html");

            excludedTypes.Remove(DataType.JsonElement.ToString());
            excludedTypes.Remove(DataType.Link.ToString());

            fields = fields.Where(f => !excludedTypes.Contains(f.Type)).ToList();

            if (type == "IssueType" && !string.IsNullOrEmpty(additionalFields) && additionalFields.Contains("|"))
            {
                var objectData = additionalFields.Split('|');
                if (objectData.Count() == 2)
                {
                    string objectType = objectData[0];
                    int objectId = int.Parse(objectData[1]);
                    var assetFields = Company.FieldTypes
                        .Where(f => f.Object == objectType && f.ObjectID == objectId && !excludedTypes.Contains(f.Type))
                        .ToList();

                    fields = fields.Union(assetFields).ToList();
                }

            }
            return fields;
        }
        [Route("diagram/clone"), HttpPost]
        public HttpResponseMessage CloneWorkflowDiagramModel(core.entities.Workflow.Type workflowType)
        {
            try
            {
                var otype = Company.Filter<core.entities.Workflow.Type>(i => i.UID == workflowType.UID).SingleOrDefault();
                if (otype == null || (otype.State != core.enums.State.Active && otype.State != core.enums.State.InActive))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.WorkflowtypeUIDNotFound, workflowType.UID.ToString()));

                //Workflow type creation

                var @type = new d360.core.entities.Workflow.Type();
                List<WorkflowActivityType> tokenTypes = new List<WorkflowActivityType>()
                    {
                        WorkflowActivityType.Form,
                        WorkflowActivityType.EmailNotification,
                        WorkflowActivityType.HTTPRequest
                    };

                @type.ID = 0;
                @type.CreatedBy = Company.CurrentResourceID;
                @type.CreatedOn = DateTime.UtcNow;
                @type.UpdatedBy = Company.CurrentResourceID;
                @type.UpdatedOn = DateTime.UtcNow;
                @type.Name = otype.Name + " (Copy)";
                @type.Description = otype.Description;
                @type.State = otype.State;

                Company.Add(@type);
                Company.SaveChanges();

                var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == otype.ID).OrderByDescending(v => v.Version).First();
                var omodel = GetWorkflowDiagram(0, workflowType.UID, currentVersion?.Version);

                var @version = new WorkflowVersion();
                @version.ID = 0;
                @version.TypeID = @type.ID;
                @version.CreatedBy = Company.CurrentResourceID;
                @version.CreatedOn = DateTime.UtcNow;
                @version.UpdatedBy = Company.CurrentResourceID;
                @version.UpdatedOn = DateTime.UtcNow;
                @version.Version = 1;

                Company.Add(@version);

                Company.SaveChanges();


                var @event = new WorkflowEventRegistration();

                @event.ID = 0;
                @event.Object = omodel.Event.Object;
                @event.ObjectID = omodel.Event.ObjectID;
                @event.TypeID = @type.ID;
                @event.ChangeType = omodel.Event.ChangeType;
                @event.Condition = omodel?.Event?.Condition ?? "";
                @event.Settings = omodel?.Event?.Settings ?? "";
                @event.State = core.enums.State.Active;

                Company.Add(@event);
                Company.SaveChanges();




                Dictionary<int, int> keyMapping = new Dictionary<int, int>();

                omodel.Nodes.ForEach(n =>
                {
                    int key = 0;
                    int.TryParse(n.Key, out key);

                    var step = new WorkflowVersionStep();
                    step.ID = 0;
                    step.Name = n.Name;
                    step.StepType = n.StepType;
                    step.ActivityType = n.ActivityType;
                    step.XPosition = n.XPosition;
                    step.YPosition = n.YPosition;
                    step.VersionID = @version.ID;
                    step.Settings = n.Settings;
                    step.State = core.enums.State.Active;

                    if (string.IsNullOrEmpty(n.Fields))
                        step.Fields = null;
                    else
                        step.Fields = n.Fields;

                    Company.Add(step);
                    Company.SaveChanges();
                    keyMapping.Add(key, step.ID);
                });


                //2nd loop to handle mappings
                omodel.Nodes.ForEach(n =>
                {
                    MapNodeSettingsAndTokens(n, keyMapping);
                });
                Company.SaveChanges();

                if (omodel?.Links?.Count > 0)
                {
                    omodel.Links.ForEach(l =>
                    {
                        int from = 0;
                        int to = 0;

                        int.TryParse(l.FromKey, out from);
                        int.TryParse(l.ToKey, out to);

                        var link = new WorkflowVersionStepTransition();

                        link.FromVersionStepID = keyMapping[from];
                        link.ToVersionStepID = keyMapping[to];
                        link.Name = l.Name;
                        link.TransitionType = l.TransitionType;
                        //need to map new form conditions to their appropriate step id's 

                        l.Condition = MapWorkflowConditionsFromXml(l.Condition, keyMapping);

                        link.Condition = l.Condition;
                        link.Settings = l.Settings;
                        link.FromPortID = l.FromPortID;
                        link.ToPortID = l.ToPortID;
                        link.State = core.enums.State.Active;

                        Company.Add(link);

                    });
                    Company.SaveChanges();
                }



                return Request.CreateResponse(HttpStatusCode.OK, @type.UID);

            }
            catch (Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, e);
            }


        }


        [Route("diagram/save"), HttpPost]
        public HttpResponseMessage PostWorkflowDiagramModel(WorkflowDiagramModel model)
        {

            int versionID = 0;
            bool newVersion = false;

            try
            {

                if (model.Type != null)
                {
                    #region Type

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
                        type.Description = model.Type.Description;
                        type.State = model.Type.State;

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

                        if (model.Type.PublishedVersionID != null)
                        {
                            type.PublishedVersionID = versionID;
                            Company.SaveChanges();
                        }
                    }
                    else
                    {
                        var type = Company.WorkflowTypes.Find(model.Type.ID);
                        bool isActiveStatusChanged = type.State != model.Type.State;

                        type.Name = model.Type.Name;
                        type.Description = model.Type.Description;
                        type.State = model.Type.State;
                        type.UpdatedOn = DateTime.UtcNow;
                        type.UpdatedBy = Company.CurrentResourceID;


                        var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).FirstOrDefault();
                        int version = currentVersion.Version;

                        versionID = currentVersion.ID;

                        //Create new workflow version for every save except when we are changing status from active to inactive or vice versa
                        bool isSameVersion = currentVersion.ID == type.PublishedVersionID;
                        if (model.Nodes.Count > 0 && model.Links.Count > 0 && (isSameVersion || model.Type.PublishedVersionID == -1) && !isActiveStatusChanged)
                        {
                            if (isSameVersion)
                            {
                                currentVersion = new WorkflowVersion();
                                currentVersion.TypeID = type.ID;
                                currentVersion.CreatedBy = Company.CurrentResourceID;
                                currentVersion.CreatedOn = DateTime.UtcNow;
                                currentVersion.UpdatedBy = Company.CurrentResourceID;
                                currentVersion.UpdatedOn = DateTime.UtcNow;
                                currentVersion.Version = version + 1;

                                Company.WorkflowVersions.Add(currentVersion);
                                Company.SaveChanges();

                                newVersion = true;
                                versionID = currentVersion.ID;
                            }

                            //set new published version
                            if (model.Type.PublishedVersionID == -1)
                            {
                                type.PublishedVersionID = currentVersion.ID;
                            }

                        }

                        Company.SaveChanges();
                    }

                    #endregion

                    #region Event

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
                            @event.State = core.enums.State.Active;

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

                    #endregion


                    List<FieldType> fieldTypes = GetFieldsForDiagramModel(model);
                    Dictionary<int, int> keyMapping = new Dictionary<int, int>();
                    List<WorkflowActivityType> tokenTypes = new List<WorkflowActivityType>()
                    {
                        WorkflowActivityType.Form,
                        WorkflowActivityType.EmailNotification,
                        WorkflowActivityType.HTTPRequest
                    };

                    if (newVersion)
                    {
                        #region Create New Version

                        if (model?.Nodes?.Count > 0)
                        {

                            model.Nodes.ForEach(n =>
                            {
                                int id = 0;
                                int.TryParse(n.Key, out id);

                                var step = new WorkflowVersionStep();
                                step.ID = 0;
                                step.Name = n.Name ?? "";
                                step.StepType = n.StepType;
                                step.ActivityType = n.ActivityType;
                                step.XPosition = n.XPosition;
                                step.YPosition = n.YPosition;
                                step.VersionID = versionID;
                                step.Settings = this.FormatMessageBodyTemplate(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Settings).ToString());
                                step.State = core.enums.State.Active;

                                if (string.IsNullOrEmpty(n.Fields))
                                    step.Fields = null;
                                else
                                    step.Fields = this.FormatFormDescription(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Fields).ToString());

                                Company.Add(step);
                                Company.SaveChanges();
                                keyMapping.Add(id, step.ID);
                            });

                            //2nd loop to handle mappings
                            model.Nodes.ForEach(n =>
                            {
                                MapNodeSettingsAndTokens(n, keyMapping);
                            });
                            Company.SaveChanges();
                        }

                        if (model?.Links?.Count > 0)
                        {
                            model.Links.ForEach(l =>
                            {
                                int from = 0;
                                int to = 0;

                                int.TryParse(l.FromKey, out from);
                                int.TryParse(l.ToKey, out to);

                                var link = new WorkflowVersionStepTransition();

                                link.FromVersionStepID = keyMapping[from];
                                link.ToVersionStepID = keyMapping[to];
                                link.Name = l.Name;
                                link.FromVersionStepID = keyMapping[from];
                                link.ToVersionStepID = keyMapping[to];
                                link.Name = l.Name ?? "";
                                link.TransitionType = l.TransitionType;

                                //need to map new form conditions to their appropriate step id's 
                                l.Condition = MapWorkflowConditions(l.Condition, keyMapping);

                                link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString();
                                link.Settings = JsonConvert.DeserializeXNode(l.Settings).ToString();
                                link.FromPortID = l.FromPortID;
                                link.ToPortID = l.ToPortID;
                                link.State = core.enums.State.Active;

                                Company.Add(link);

                            });
                        }

                        #endregion
                    }
                    else
                    {
                        #region Modify Existing Version

                        var existingSteps = Company.WorkflowVersionSteps.Where(s =>
                            s.State == core.enums.State.Active
                            && s.VersionID == versionID)
                            .ToList();

                        var existingLinks = new List<WorkflowVersionStepTransition>();

                        existingSteps.ForEach(s =>
                        {
                            var transition = Company.WorkflowVersionStepTransitions.Where(t => t.FromVersionStepID == s.ID && t.State == core.enums.State.Active);
                            if (transition != null)
                                existingLinks.AddRange(transition);
                        });

                        existingLinks.ForEach(l =>
                        {
                            if (existingSteps.Count(s => s.ID == l.ToVersionStepID) < 1)
                            {
                                l.State = core.enums.State.Deleted;
                                var fromLinks = Company.WorkflowVersionStepTransitions.Where(t => t.FromVersionStepID == l.ToVersionStepID && t.State == core.enums.State.Active).ToList();

                                fromLinks.ForEach(f => { f.State = core.enums.State.Deleted; });
                            }

                        });

                        Company.SaveChanges();



                        if (model?.Nodes?.Count > 0)
                        {
                            model.Nodes.ForEach(n =>
                            {

                                int id = 0;
                                int.TryParse(n.Key, out id);

                                //new step
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
                                    step.Settings = this.FormatMessageBodyTemplate(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Settings).ToString());
                                    step.State = core.enums.State.Active;

                                    if (string.IsNullOrEmpty(n.Fields))
                                        step.Fields = null;
                                    else
                                        step.Fields = this.FormatFormDescription(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Fields).ToString());

                                    Company.Add(step);
                                    Company.SaveChanges();
                                    keyMapping.Add(id, step.ID);
                                }
                                //modify exsiting step
                                else if (id > 0)
                                {
                                    var node = Company.WorkflowVersionSteps.Find(id);

                                    var existing = existingSteps.Find(s => s.ID == id);
                                    if (existing != null) existingSteps.Remove(existing);

                                    if (node != null)
                                    {
                                        node.ActivityType = n.ActivityType;
                                        node.Name = n.Name ?? "";
                                        node.StepType = n.StepType;
                                        node.XPosition = n.XPosition;
                                        node.YPosition = n.YPosition;
                                        node.VersionID = versionID;
                                        node.Settings = this.FormatMessageBodyTemplate(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Settings).ToString());

                                        if (string.IsNullOrEmpty(n.Fields))
                                            node.Fields = null;
                                        else
                                            node.Fields = this.FormatFormDescription(model.Event.Object, fieldTypes, JsonConvert.DeserializeXNode(n.Fields).ToString());

                                        keyMapping.Add(id, id);
                                    }
                                }
                            });
                            Company.SaveChanges();

                            //2nd loop to handle mappings
                            model.Nodes.ForEach(n =>
                            {
                                MapNodeSettingsAndTokens(n, keyMapping);
                            });
                            Company.SaveChanges();
                        }



                        if (existingSteps.Count > 0 && model?.Nodes?.Count > 0)
                        {
                            //mark anything left as deleted
                            existingSteps.ForEach(s => s.State = core.enums.State.Deleted);
                            Company.SaveChanges();
                        }

                        if (model?.Links?.Count > 0)
                        {
                            model.Links.ForEach(l =>
                            {
                                int from = 0;
                                int to = 0;

                                int.TryParse(l.FromKey, out from);
                                int.TryParse(l.ToKey, out to);

                                bool fromNew = (from < 0);
                                bool toNew = (to < 0);

                                var link = Company.WorkflowVersionStepTransitions.SingleOrDefault(v => v.FromVersionStepID == from && v.ToVersionStepID == to && v.State == core.enums.State.Active);

                                if (fromNew || toNew || link == null)
                                {
                                    if (link == null)
                                        link = new WorkflowVersionStepTransition();

                                    link.FromVersionStepID = keyMapping[from];
                                    link.ToVersionStepID = keyMapping[to];
                                    link.Name = l.Name ?? "";
                                    link.TransitionType = l.TransitionType;

                                    //need to map new form conditions to their appropriate step id's 
                                    l.Condition = MapWorkflowConditions(l.Condition, keyMapping);

                                    link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString();
                                    link.Settings = JsonConvert.DeserializeXNode(l.Settings).ToString();
                                    link.FromPortID = l.FromPortID;
                                    link.ToPortID = l.ToPortID;
                                    link.State = core.enums.State.Active;

                                    Company.Add(link);
                                }
                                else
                                {
                                    var existing = existingLinks.Find(t => t.FromVersionStepID == link.FromVersionStepID && t.ToVersionStepID == link.ToVersionStepID);
                                    if (existing != null) existingLinks.Remove(existing);

                                    if (link != null)
                                    {
                                        link.Name = l.Name ?? "";
                                        link.TransitionType = l.TransitionType;
                                        link.FromPortID = l.FromPortID;
                                        link.ToPortID = l.ToPortID;

                                        //need to map new form conditions to their appropriate step id's 
                                        l.Condition = MapWorkflowConditions(l.Condition, keyMapping);

                                        link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString();
                                        link.Settings = JsonConvert.DeserializeXNode(l.Settings).ToString();
                                    }
                                }
                            });

                            Company.SaveChanges();
                        }

                        if (existingLinks.Count > 0 && model?.Links?.Count > 0)
                        {
                            existingLinks.ForEach(l => l.State = core.enums.State.Deleted);
                            Company.SaveChanges();
                        }

                        #endregion
                    }

                }

                return Request.CreateResponse(HttpStatusCode.OK, model.Type.ID);

            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        private List<FieldType> GetFieldsForDiagramModel(WorkflowDiagramModel model)
        {
            List<FieldType> fieldTypes = this.getFieldTypes(model.Event.ObjectID, model.Event.Object, true);

            //Get asset fields for action
            if (model.Event.Object == "IssueType")
            {
                string IssueObjectType = string.Empty;
                int IssueObjectId = -1;
                if (!string.IsNullOrEmpty(model?.Event?.Condition))
                {
                    string asXML;
                    if (model.Event.Condition.TrimStart().First() == '{')
                        asXML = JsonConvert.DeserializeXNode(model.Event.Condition).ToString();
                    else
                        asXML = model.Event.Condition;

                    var parsedConditions = XmlToDynamic(GetConditionLabels(asXML));
                    if (parsedConditions != null && parsedConditions.Condition != null)
                    {
                        var conditions = parsedConditions.Condition;
                        if (Convert.ToInt32(conditions.Count) >= 2)
                        {
                            if (conditions[0]["@ContextualFieldID"] == "IssueObject" && conditions[1]["@ContextualFieldID"] == "IssueObjectID")
                            {
                                IssueObjectType = conditions[0]["@Value"];
                                IssueObjectId = Convert.ToInt32(conditions[1]["@Value"]);
                            }
                        }
                    }
                }
                if (IssueObjectId > 0 && !string.IsNullOrEmpty(IssueObjectType))
                {
                    fieldTypes = fieldTypes.Union(this.getFieldTypes(IssueObjectId, IssueObjectType, true)).ToList();
                }
            }

            return fieldTypes;
        }


        private string FormatFormDescription(string type, List<FieldType> fieldTypes, string data)
        {
            dynamic fields = XmlToDynamic(data);
            if (fields != null && fields.form != null && fields.form["@description"] != null)
            {
                fields.form["@description"] = FormatWorkflowProperty(fields.form["@description"].ToString(), fieldTypes);
                return JsonConvert.DeserializeXNode(fields.ToString(), "fields").ToString();
            }
            return data;
        }

        private string DeFormatFormDescription(string type, List<FieldType> fieldTypes, string data)
        {
            dynamic fields = XmlToDynamic(data);
            if (fields != null && fields.form != null && fields.form["@description"] != null)
            {
                fields.form["@description"] = DeFormatWorkflowProperty(fields.form["@description"].ToString(), fieldTypes);
                return JsonConvert.DeserializeXNode(fields.ToString(), "fields").ToString();
            }
            return data;
        }

        private string FormatMessageBodyTemplate(string type, List<FieldType> fieldTypes, string data)
        {
            dynamic settings = XmlToDynamic(data);

            if (settings != null && (settings.MessageBodyTemplate != null || settings.MessageSubjectTemplate != null || settings.HTTPRequest != null))
            {
                if (settings.MessageBodyTemplate != null)
                {
                    settings.MessageBodyTemplate = FormatWorkflowProperty(settings.MessageBodyTemplate.ToString(), fieldTypes);
                }

                if (settings.MessageSubjectTemplate != null)
                {
                    settings.MessageSubjectTemplate = FormatWorkflowProperty(settings.MessageSubjectTemplate.ToString(), fieldTypes);
                }

                if (settings.HTTPRequest != null)
                {
                    if (settings.HTTPRequest.Body != null)
                    {
                        settings.HTTPRequest.Body = FormatWorkflowProperty(settings.HTTPRequest.Body.ToString(), fieldTypes);
                    }
                    if (settings.HTTPRequest.Url != null)
                    {
                        settings.HTTPRequest.Url = FormatWorkflowProperty(settings.HTTPRequest.Url.ToString(), fieldTypes);
                    }
                }

                return JsonConvert.DeserializeXNode(settings.ToString(), "settings").ToString();

            }

            return data;
        }

        private string DeFormatMessageBodyTemplate(string type, List<FieldType> fieldTypes, string data)
        {
            dynamic settings = XmlToDynamic(data);
            if (settings != null && (settings.MessageBodyTemplate != null || settings.MessageSubjectTemplate != null || settings.HTTPRequest != null))
            {
                if (settings.MessageBodyTemplate != null)
                {
                    settings.MessageBodyTemplate = DeFormatWorkflowProperty(settings.MessageBodyTemplate.ToString(), fieldTypes);
                }

                if (settings.MessageSubjectTemplate != null)
                {
                    settings.MessageSubjectTemplate = DeFormatWorkflowProperty(settings.MessageSubjectTemplate.ToString(), fieldTypes);
                }

                if (settings.HTTPRequest != null)
                {
                    if (settings.HTTPRequest.Body != null)
                    {
                        settings.HTTPRequest.Body = DeFormatWorkflowProperty(settings.HTTPRequest.Body.ToString(), fieldTypes);
                    }
                    if (settings.HTTPRequest.Url != null)
                    {
                        settings.HTTPRequest.Url = DeFormatWorkflowProperty(settings.HTTPRequest.Url.ToString(), fieldTypes);
                    }
                }

                return JsonConvert.DeserializeXNode(settings.ToString(), "settings").ToString();
            }

            return data;
        }


        private string FormatWorkflowProperty(string msg, List<FieldType> fieldTypes)
        {
            fieldTypes.ForEach(x =>
            {
                var fieldType = x.Object == "IssueType" ? "Action Field" : "Asset Field";
                var f = "[" + fieldType + " :: " + x.Name + "]";
                var t = (x.Type == DataType.JsonElement.ToString() ? "[JSON" : "[FIELD") + x.ID + "]";
                msg = msg.Replace(f, t);
            });


            return msg;
        }

        private string DeFormatWorkflowProperty(string msg, List<FieldType> fieldTypes)
        {
            fieldTypes.ForEach(x =>
            {
                var fieldType = x.Object == "IssueType" ? "Action Field" : "Asset Field";
                var f = "[" + fieldType + " :: " + x.Name + "]";
                var t = (x.Type == DataType.JsonElement.ToString() ? "[JSON" : "[FIELD") + x.ID + "]";
                msg = msg.Replace(t, f);
            });
            return msg;
        }


        [Route("type/{id:int}/{uid:Guid}/delete")]
        public HttpResponseMessage DeleteWorkflow(int id,Guid? uid)
        {
            core.entities.Workflow.Type type;
            if (uid.HasValue && uid.Value != Guid.Empty)
                type= Company.Filter<core.entities.Workflow.Type>(i => i.UID == uid.Value).SingleOrDefault();
            else
                type = Company.WorkflowTypes.Find(id);

            if (type == null)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.WorkflowtypeidNotFound, id.ToString()));

            if (type.State == core.enums.State.Deleted)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format(WorkflowApiMessages.WorkflowIDAlreadyDeleted, id.ToString()));

            type.State = core.enums.State.Deleted;
            type.UpdatedOn = DateTime.UtcNow;
            type.UpdatedBy = Company.CurrentResourceID;
            Company.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, id);
        }

        [Route("type/{id:int}/versions")]
        public HttpResponseMessage GetVersions(int id)
        {
            return Request.CreateResponse(HttpStatusCode.OK, Company.WorkflowVersions.Where(v => v.TypeID == id).ToList());
        }

        [Route("type/{typeId:int}/myinstances")]
        public HttpResponseMessage GetAssignedWorkflowInstances(int typeId, int version, int stepId, int resourceId = 0)
        {
            try
            {
                //get workflow instances of the type specified assigned to the current user
                var sql = @"    select
	                                wt.name as 'WorkflowName'
	                                ,wi.[object] as 'Object'
	                                ,wi.[objectid] as 'ObjectID'
	                                ,wi.startedOn as 'StartedOn'
	                                ,wi.startedBy as 'StartedByResourceID'
                                    ,wi.id as 'ItemID'
	                                ,gr.firstName + ' ' + gr.lastName as 'StartedBy'
	                                ,case 
										when wi.[object] = 'Issue' then it.Name
										else assettype.name
									end as 'TypeName'
	                                ,assettype.[Object] as 'ObjectType'
	                                ,assettype.ObjectID as 'ObjectTypeID'	                                
                                    ,case 
                                        when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)')
                                        else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)')
                                    end as 'ObjectName'
	                                ,wis.id as 'ItemStepID'
	                                ,wvs.name as 'StepName'
	                                ,wvs.steptype as 'StepType'
	                                ,wvs.activitytype as 'ActivityType'
                                    ,iss.[object] as 'IssueObject'
									,iss.[objectid] as 'IssueObjectID'
                                    ,utility.getassetdisplayvalue(cod.id) as 'IssueObjectName'  
                                    ,case when wi.[object] = 'Issue' then utility.getassetdisplayvalue(cod.id)
                                      when wi.[object] = 'Intersect' then coalesce(utility.deriveintersectname(wi.objectid), '(unknown relationship)')
                                        else coalesce(utility.getassetdisplayvalue(ass.id),'(unknown)')
                                    end as Name,
                                    wvs.Settings.query('settings/FormResponseType').value('.', 'varchar(50)') as 'responseType',
	                                itemCount.assignedCount as 'countAssigned'
                                from
	                                [workflow].[type] wt
	                                inner join [workflow].[version] wv on (wt.id = wv.typeid)
	                                inner join [workflow].[item] wi on (wv.id = wi.versionid)	                                
	                                left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid])
									left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)
	                                inner join [workflow].[itemstep] wis on(wis.itemid = wi.id and wis.completedon is null)
	                                inner join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
	                                inner join [workflow].[itemassignment] wia on (wia.itemid = wi.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r and (wia.itemstepid = wis.id or wia.itemstepid is null))
                                    outer apply( 
										SELECT count(*) as 'assignedCount' from [workflow].[itemassignment] 
											WIC WHERE WIC.itemid = wi.id and (WIC.itemstepid = wis.id or WIC.itemstepid is null)
										) itemCount(assignedCount)                                    
                                    inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid)
                                    left outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue')
                                    left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                                    left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id)                                    
                                where
                                    wt.id = @typeId and wi.completedon is null and wvs.steptype = 2 and wvs.activitytype = 3 
                                    and wv.[version]=@verid and wvs.id = @sid  
                                    order by StartedOn desc
                           ";

                var workflow = Company.WorkflowTypes.Where(x => x.ID == typeId).FirstOrDefault();

                var items = Company.Query<dynamic>(sql, new { r = (resourceId > 0 ? resourceId : Company.CurrentResourceID), typeId, verid = version, sid = stepId });

                return Request.CreateResponse(new { items, workflow });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [Route("type/{typeId:int}/myinstances/summary")]
        public HttpResponseMessage GetAssignedWorkflowInstancesHeader(int typeId, int version, int stepId, int resourceId = 0)
        {
            try
            {
                //get workflow instances of the type specified assigned to the current user
                var sql = @"    select
                                    top 1 wi.[object] as 'ObjectName',
                                    wvs.name as 'StepName',
                                    case when wi.[object] = 'Issue' then it.Name
                                    else assettype.name
                                    end as 'TypeName' ,
                                    wv.[version] as 'Version',
									case when wvs.Settings.value('/settings[1]/SendFormEmail[1]/text()[1]','varchar(10)') = 'true' then
										cast(1 as bit)
									else
										cast(0 as bit)
									end as 'SendFormEmail'
                                from
	                                [workflow].[type] wt
	                                inner join [workflow].[version] wv on (wt.id = wv.typeid)
	                                inner join [workflow].[item] wi on (wv.id = wi.versionid)	                                
	                                left join [dbo].asset ass on(ass.[object] = wi.[object] and ass.[objectid] = wi.[objectid])
									left join [dbo].assettype assettype on(ass.assettypeid = assettype.id)
	                                left join [workflow].[itemstep] wis on(wis.itemid = wi.id )
	                                left join [workflow].[itemassignment] wia on(wia.itemid = wi.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r)
	                                left join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
                                    inner join [reporting].global_resource gr on (wi.startedBy = gr.resourceid)
                                    left outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue')
                                    left outer join [dbo].[asset] cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                                    left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id)                                    
                                where
                                    wt.id = @typeId  and wvs.steptype = 2 and wvs.activitytype = 3 
                                    and wv.[version]=@verid and wvs.id = @sid  
                                   
                           ";



                var item = Company.Query<WorkflowAssignmentSummary>(sql, new { r = (resourceId > 0 ? resourceId : Company.CurrentResourceID), typeId, verid = version, sid = stepId }).FirstOrDefault<WorkflowAssignmentSummary>();

                return Request.CreateResponse(new { item });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("type/{typeId:int}/haspendingitems"), HttpGet]
        public HttpResponseMessage HasPendingWorkflowsItems(int typeId)
        {
            var sql = $@"		Select count(*)        from workflow.type t
                            inner join workflow.version v on v.typeid = t.id
                            inner join workflow.versionstep vs on vs.versionid = v.id
                            inner join workflow.itemstep s on s.stepid = vs.id and s.CompletedOn is null
                            where  t.State =1 and t.id= {typeId}";
            int result = Company.Query<int>(sql).SingleOrDefault();

            return Request.CreateResponse(HttpStatusCode.OK, result != 0);
        }
        [Route("procedures"), HttpGet]
        public IQueryable GetWorkflowProcedures()
        {
            return Company.WorkflowTaskProcedures;
        }

        [Route("typelist"), HttpGet]
        public HttpResponseMessage GetWorkflowsByTypeList(string types, string filteredObject = null, int? filteredObjectId = null)
        {
            string issueSql = "", typeSql = "t.id in ({0}) and";

            if (types != null && types.ToLower().Trim() == "all")
                typeSql = "";

            //should only ever be comma separated list of numbers, remove anything else
            types = Regex.Replace(types ?? "", "[^0123456789, ]", string.Empty);
            types = types.Trim().TrimEnd(',');

            if (string.IsNullOrWhiteSpace(types))
                types = "-1";

            if (!string.IsNullOrEmpty(typeSql))
                typeSql = string.Format(typeSql, types);

            //get issue workflows related to the object as well
            if (filteredObjectId != null)
            {
                issueSql = @" and e.[Object] != 'IssueType' or (e.Object = 'IssueType' and t.id in
					        (select t.id from workflow.type t
	                            inner join workflow.eventregistration e on e.typeid = t.id and e.object = 'IssueType'
	                            inner join workflow.[version] v on t.id = v.typeid
	                            inner join workflow.item i on i.versionid = v.id
                                inner join issue s on s.id = i.objectid and i.object = 'Issue'
                                inner join asset a on a.object = s.object and a.objectid = s.objectid
                                inner join assettype tt on tt.id = a.assettypeid
	                            where t.state <> 3 and s.object = @filteredObject and s.objectid = @filteredObjectId))";
            }
            var sql = string.Format(QueryConstants.WorkflowTypeList, typeSql, issueSql);
            var results = Company.Query<dynamic>(sql, new { filteredObject, filteredObjectId }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }


        [Route("versionstep/history/{id:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowVersionStepHistory(int id, string filteredObject = null, int? filteredObjectId = null)
        {
            var sql = QueryConstants.WorkflowVersionStepHistory;

            if (filteredObject != null && filteredObjectId != null)
            {
                if (filteredObject == "Issue")
                    sql = string.Format(sql, "inner join workflow.item m on m.id = i.itemid", "left join issue s on m.object = 'Issue' and s.id = m.objectID and s.object = @filteredObject and s.objectid = @filteredObjectId");
                else
                    sql = string.Format(sql, "inner join workflow.item m on m.id = i.itemid and m.object = @filteredObject and m.objectid = @filteredObjectId", "left join issue s on m.object = 'Issue' and s.id = m.objectID");
            }
            else
            {
                sql = string.Format(sql, "left join workflow.item m on m.id = i.itemid", "left join issue s on m.object = 'Issue' and s.id = m.objectID");
            }


            var results = Company.Query<dynamic>(sql, new { id, filteredObject, filteredObjectId }).ToList();

            results.ForEach(r =>
            {
                r.SettingsObject = XmlToDynamic(r.Settings);
                r.FieldsObject = XmlToDynamic(r.Fields);
            });

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("versionstep/history/{id:int}/excel.xls"), HttpGet]
        public HttpResponseMessage GetWorkflowVersionStepHistoryExcel(int id, string filteredObject = null, int? filteredObjectId = null)
        {

            var sql = QueryConstants.WorkflowVersionStepHistory;

            if (filteredObject != null && filteredObjectId != null)
            {
                if (filteredObject == "Issue")
                    sql = string.Format(sql, "inner join workflow.item m on m.id = i.itemid", "left join issue s on m.object = 'Issue' and s.id = m.objectID and s.object = @filteredObject and s.objectid = @filteredObjectId");
                else
                    sql = string.Format(sql, "inner join workflow.item m on m.id = i.itemid and m.object = @filteredObject and m.objectid = @filteredObjectId", "left join issue s on m.object = 'Issue' and s.id = m.objectID");
            }
            else
            {
                sql = string.Format(sql, "left join workflow.item m on m.id = i.itemid", "left join issue s on m.object = 'Issue' and s.id = m.objectID");
            }

            var results = Company.Query<dynamic>(sql, new { id }).ToList();

            #region Header

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "History");

            int index = 1;
            document.SetCellValue(1, index++, "Object");
            document.SetCellValue(1, index++, "Status");
            document.SetCellValue(1, index++, "Started On");
            document.SetCellValue(1, index++, "Completed On");
            document.SetCellValue(1, index++, "Started By");
            document.SetCellValue(1, index++, "Comment");

            #endregion

            int rowNumber = 1;
            foreach (var row in results)
            {
                index = 1;
                rowNumber++;
                document.SetCellValue(rowNumber, index++, (string)row.Name ?? "");
                document.SetCellValue(rowNumber, index++, (string)row.Status ?? "");
                document.SetCellValue(rowNumber, index++, row.StartedOn?.ToString() ?? "");
                document.SetCellValue(rowNumber, index++, row.CompletedOn?.ToString() ?? "");
                document.SetCellValue(rowNumber, index++, (string)row.StartedBy ?? "");
                document.SetCellValue(rowNumber, index++, (string)row.Comment ?? "");
            }

            document.AutoFitColumn(1, index);

            var stream = new MemoryStream();
            document.SaveAs(stream);
            stream.Position = 0;
            HttpResponseMessage result = null;
            result = Request.CreateResponse(HttpStatusCode.OK);
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Step History {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [Route("versionstep/form/lookups/{objectType}/{objectId:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowVersionStepFormLookups(string objectType, int objectId, string issueObject = null, int? issueObjectId = null)
        {
            bool hasIssueObject = !string.IsNullOrEmpty(issueObject);

            var sql = $@"select ft.ID as value, {(hasIssueObject ? " 'Action Field :: ' + " : "")} ft.FriendlyName + ' (' + coalesce( ri.Name, ft.LookupObjectType) + ')' as [label] from 
                 FieldType ft
                 left join AssetType ri on ri.objectid = ft.lookupobjectid and ri.[object] = 'ReferenceItemType' and ft.LookupObjectType = 'ReferenceItem'
                 where ft.Object = @objectType and ft.ObjectID = @objectId and ft.Type = 'Lookup' and ft.LookupObjectId > 0";

            if (hasIssueObject)
            {
                sql += @" union all
                select ft.ID as value, ft.FriendlyName + ' (' + coalesce( ri.Name, ft.LookupObjectType) + ')' as [label] from 
                 FieldType ft
                 left join AssetType ri on ri.objectid = ft.lookupobjectid and ri.[object] = 'ReferenceItemType' and ft.LookupObjectType = 'ReferenceItem'
                 where ft.Object = @issueObject and ft.ObjectID = @issueObjectId and ft.Type = 'Lookup' and ft.LookupObjectId > 0
                order by 2";
            }
            else
            {
                sql += " order by ft.FriendlyName";
            }

            var results = Company.Query<dynamic>(sql, new { objectType,  objectId, issueObject, issueObjectId });

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("versionstep/events/{id:int}")]
        public HttpResponseMessage GetWorkflowVersionStepEventInfo(int id)
        {
            var results = Company.Query<dynamic>(@"select 
								max(s.id) as VersionStepID, max(s.Name) as Name, 
								 sum(i.numberofevents) as NumberOfEvents
								  from workflow.versionstep s
							left join workflow.version v on v.id = s.versionid
							left join workflow.item i on i.versionid = v.id
							where v.id = @id
							group by s.id", new { id }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("openactions"), HttpGet]
        public HttpResponseMessage GetWorkflowActions(string types)
        {

            var resourceId = Company.CurrentResourceID;

            types = Regex.Replace(types ?? "", "[^0123456789, ]", string.Empty);

            types = types.Trim().TrimEnd(',');

            if (string.IsNullOrWhiteSpace(types))
                types = "-1";

            var sql = string.Format(QueryConstants.WorkflowAssignments, types);
            var results = Company.Query<dynamic>(sql, new { resourceId = resourceId }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);

        }

        [Route("lastexecution/{id:int}/{uid:Guid}"), HttpDelete]
        public HttpResponseMessage DeleteLastExecution(int id, Guid? uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Forbidden, new Exception(ApiMessages.AccessDenied));

            if (id == 0 && uid.HasValue && uid.Value != Guid.Empty)
                id = Company.Filter<core.entities.Workflow.Type>(i => i.UID == uid.Value).SingleOrDefault().ID;

            if (!Company.WorkflowEventRegistrations.Any(x => x.TypeID == id))
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception(string.Format(WorkflowApiMessages.WorkflowtypeidNotFound, id.ToString())));
            }

            var workflow = Company.WorkflowEventRegistrations.First(x => x.TypeID == id);

            workflow.LastExecuted = null;

            Company.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, workflow);

        }

        [Route("item/{itemId:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowItemSteps(int itemId)
        {
            var item = Company.WorkflowItems.FirstOrDefault(i => i.ID == itemId);

            if (item == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception(WorkflowApiMessages.ItemNotFound));
            }

            var results = Company.Query<WorkflowItemStepDetail>(QueryConstants.WorkflowItemSteps, new { itemId }).ToList();

            foreach (var result in results)
            {
                result.FieldsObject = (WorkflowItemStepDetail.FieldsModel)new XmlSerializer(typeof(WorkflowItemStepDetail.FieldsModel)).Deserialize(new StringReader(result.Fields));
                var fields = result.FieldsObject;

                if (result.ActivityType == WorkflowActivityType.Form && result.Complete == false)
                {
                    var assignmentIds = Company.WorkflowItemAssignments.Where(x => x.ItemStepID == result.ID).Select(x => new { x.ResourceObject, x.ResourceObjectID });
                    var formattedUserList = Company.GlobalReportingResources.Where(x => assignmentIds.Any(a=>a.ResourceObjectID == x.ResourceID)).ToList().Select(x => x.FullName);
                    result.Assignee = string.Join(", ", formattedUserList);

                }
            }

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        private void SetWorkFlowStepRelationshipAssets(dynamic form)
        {
            string assets = string.Empty;
            if (form != null && form.field != null)
            {
                JArray sfields = new JArray(form.field);
                JObject jo = sfields.Children<JObject>()
                        .FirstOrDefault(o => o["@fieldtype"] != null && o["@fieldtype"].ToString() == "relationshiptype");

                string changedTypes = jo != null && jo["@value"] != null ? jo["@value"].ToString() : null;
                if (changedTypes != null)
                {

                    foreach (var changedType in changedTypes.Split(','))
                    {
                        var types = changedType.Split('|');
                        if (types.Length == 2)
                        {
                            var typeSql = @"Select DisplayValue from assetdetail where object =@obj and objectId=@objId";
                            string displayValue = Company.Query<string>(typeSql, new { obj = types[0].Replace("Type", ""), objId = types[1] }).FirstOrDefault();
                            displayValue = string.IsNullOrEmpty(displayValue) ? "Not Found" : displayValue;
                            assets += $"/ {displayValue} ";
                        }
                    }
                    if (assets.StartsWith("/"))
                        assets = assets.Substring(1, assets.Length - 1);

                    jo["@displayvalue"] = assets;
                    form.field = sfields;
                }

            }

        }

        private WorkflowStepRelationshipChange GetWorkFlowStepRelationshipChanges(dynamic settings, int itemId, string objectName)
        {
            WorkflowStepRelationshipChange relChange = null;
            if (settings?.RelationshipUpdate?.Relationship != null)
            {

                dynamic relations = new JArray(settings.RelationshipUpdate.Relationship);
                if (relations[0] != null)
                {
                    relChange = new WorkflowStepRelationshipChange();
                    var relation = relations[0];
                    relChange.AppendValue = relation["@AppendValue"] != null ? relation["@AppendValue"] : false;
                    relChange.ClearValue = relation["@ClearValue"] != null ? relation["@ClearValue"] : false;

                    int stepId = relation["@FormStepId"] != null ? relation["@FormStepId"] : 0;
                    if (stepId != 0)
                    {

                        var stepSql = @"select Fields from workflow.VersionStep where  Id=@stepid";
                        dynamic stepFields = Company.Query<string>(stepSql, new { stepid = stepId }).FirstOrDefault();
                        stepFields = XmlToDynamic(stepFields, false);
                        if (stepFields.fields != null && stepFields.fields.form != null && stepFields.fields.form.field != null)
                        {
                            JArray sfields = new JArray(stepFields.fields.form.field);
                            JObject jo = sfields.Children<JObject>()
                                    .FirstOrDefault(o => o["@type"] != null && o["@type"].ToString() == "relationshipType" && o["@intersectTypeId"] != null); ;

                            int IntersectTypeId = jo != null && jo["@intersectTypeId"] != null ? Convert.ToInt32(jo["@intersectTypeId"]) : 0;
                            var interceptSql = @"SELECT	
						                         ITypeName.Name AS Name
					                        FROM	IntersectType IT    
				                                cross apply dbo.GetIntersectTypeNames(IT.ID) ITypeName	
			                             where IT.ID=@intersectTypeId";
                            relChange.TypeName = Company.Query<string>(interceptSql, new { intersectTypeId = IntersectTypeId }).FirstOrDefault();
                        }

                        var itemStepSql = @"select fields from workflow.itemstep where  stepid=@stepid and itemid=@itemid";
                        dynamic itemStepFields = Company.Query<string>(itemStepSql, new { stepid = stepId, itemid = itemId }).FirstOrDefault();
                        itemStepFields = XmlToDynamic(itemStepFields, false);


                        if (itemStepFields.fields != null && itemStepFields.fields.form != null && itemStepFields.fields.form.field != null)
                        {
                            JArray sfields = new JArray(itemStepFields.fields.form.field);
                            JObject jo = sfields.Children<JObject>()
                                    .FirstOrDefault(o => o["@fieldtype"] != null && o["@fieldtype"].ToString() == "relationshiptype");

                            //var sfield = sfields[0];
                            string changedTypes = jo != null && jo["@value"] != null ? jo["@value"].ToString() : null;
                            if (changedTypes != null)
                            {
                                relChange.Relationship += $"{objectName}";
                                foreach (var changedType in changedTypes.Split(','))
                                {
                                    var types = changedType.Split('|');
                                    if (types.Length == 2)
                                    {
                                        var typeSql = @"Select DisplayValue from assetdetail where object =@obj and objectId=@objId";
                                        string displayValue = Company.Query<string>(typeSql, new { obj = types[0].Replace("Type", ""), objId = types[1] }).FirstOrDefault();
                                        displayValue = string.IsNullOrEmpty(displayValue) ? "Not Found" : displayValue;
                                        relChange.Relationship += $" / {displayValue}";
                                    }
                                }

                            }

                        }
                    }

                }

            }

            return relChange;
        }

        private List<EmailedResourceResponsibility> GetEmailResources(int assetId, List<int> responsiblities, List<string> emails)
        {

            string sql = string.Empty;
            var asset = Company.GetAssetDetail(assetId);

            if (responsiblities.Count != 0 && asset != null)
            {
                sql = $@"WITH CTE(FullName,ResourceID,ResponsibilityTypeName,Email) 
                        as 
                         (Select distinct  R.FirstName + ' ' + R.LastName as FullName, r.ResourceID,rd.ResponsibilityTypeName,R.Email
                        from ResponsibilityDetail rd
                         inner join reporting.Global_Resource R on
                         rd.resourceId = r.resourceId 
                         where r.email  IN ('{string.Join("','", emails)}')
                         and ((rd.AssetID = {assetId}) or (rd.AssetID = 0 and rd.AssetTypeID ={asset.AssetTypeID}) and rd.IsVisible=1)
                        and rd.ResponsibilityTypeID IN ( {string.Join(",", responsiblities)}))

                        SELECT cte.FullName,cte.ResourceID,cte.email,
	                        STRING_AGG(cte.ResponsibilityTypeName, ', ') WITHIN GROUP (ORDER BY cte.ResponsibilityTypeName asc) as Responsibility
                        from cte
                        group by cte.FullName,cte.ResourceID,cte.Email";
            }
            else
            {
                sql = $@"Select R.FirstName + ' ' + R.LastName as FullName, r.ResourceID,R.Email
						From reporting.Global_Resource R  
						where state=1 and email in ('{string.Join("','", emails)}')";
            }

            return Company.Query<EmailedResourceResponsibility>(sql).ToList();
        }
        private List<WorkflowStepFieldChange> GetWorkFlowStepFieldChanges(WorkflowStepDetail detail)
        {
            List<WorkflowStepFieldChange> fieldChanges = new List<WorkflowStepFieldChange>();
            if (detail.Settings != null && detail.Settings.FieldUpdate != null && detail.Settings.FieldUpdate.Field != null)
            {

                dynamic fields = new JArray(detail.Settings.FieldUpdate.Field);
                for (int i = 0; i < fields.Count; i++)
                {
                    var fieldChange = new WorkflowStepFieldChange();
                    bool isFromActionForm = false;


                    var field = fields[i];
                    int fieldTypeId = field["@FieldId"] != null ? field["@FieldId"] : 0;
                    if (fieldTypeId == 0) continue;
                    fieldChange.FormValue = field["@UseFormValue"] != null ? field["@UseFormValue"] : false;
                    fieldChange.ObjectType = field["@ObjectType"] != null ? field["@ObjectType"] : "";
                    fieldChange.UseCurrentDate = field["@UseCurrentDate"] != null ? field["@UseCurrentDate"] : false;
                    fieldChange.AppendValue = field["@AppendValue"] != null ? field["@AppendValue"] : "";
                    fieldChange.ClearValue = field["@ClearValue"] != null ? field["@ClearValue"] : "";
                    fieldChange.UseOutputValue = field["@UseOutputValue"] != null ? field["@UseOutputValue"] : false;
                    FieldType fieldType = Company.GetById<FieldType>(fieldTypeId);
                    fieldChange.FieldName = fieldType?.FriendlyName;
                    fieldChange.Type = fieldType?.Type;
                    string formFieldId = field["@FormFieldId"] != null ? field["@FormFieldId"] : null;
                    int stepId = field["@FormStepId"] != null ? field["@FormStepId"] : 0;
                    isFromActionForm = field["@IsActionForm"] != null ? bool.Parse(field["@IsActionForm"].ToString()) : false;

                    if (!isFromActionForm && fieldChange.FormValue && formFieldId != null && stepId != 0)
                    {

                        var stepSql = @"select fields from workflow.itemstep where  stepid=@stepid and itemid=@itemid";
                        dynamic stepFields = Company.Query<string>(stepSql, new { stepid = stepId, itemid = detail.ItemID }).FirstOrDefault();
                        stepFields = XmlToDynamic(stepFields, false);


                        if (stepFields.fields != null && stepFields.fields.form != null && stepFields.fields.form.Count > 1)
                        {
                            List<string> vlist = new List<string>();
                            string fieldValue = string.Empty;
                            for (int k = 0; k < stepFields.fields.form.Count; k++)
                            {
                                JArray sfields = new JArray(stepFields.fields.form[k].field);
                                JObject jo = sfields.Children<JObject>()
                                    .FirstOrDefault(o => o["@id"] != null && o["@id"].ToString() == formFieldId);
                                var displayvalue = jo != null && jo["@displayvalue"] != null ? jo["@displayvalue"].ToString() : "";
                                var fieldtype = jo != null && jo["@fieldtype"] != null ? jo["@fieldtype"].ToString() : "";
                                switch (fieldtype)
                                {
                                    case "date":
                                        fieldValue = displayvalue != "" ? Convert.ToDateTime(displayvalue).ToShortDateString() : "";
                                        break;
                                    default:
                                        if (fieldChange.AppendValue == "true")
                                            vlist.AddRange(displayvalue.Split(','));
                                        else
                                            fieldValue = displayvalue;
                                        break;
                                }
                            }

                            if (fieldChange.AppendValue == "true")
                                fieldChange.Value = string.Join(",", vlist.Distinct().ToArray());
                            else
                                fieldChange.Value = fieldValue;

                        }
                        else
                        {
                            JArray sfields = new JArray(stepFields.fields.form.field);
                            JObject jo = sfields.Children<JObject>()
                                .FirstOrDefault(o => o["@id"] != null && o["@id"].ToString() == formFieldId);
                            var displayvalue = jo != null && jo["@displayvalue"] != null ? jo["@displayvalue"].ToString() : "";
                            var fieldtype = jo != null && jo["@fieldtype"] != null ? jo["@fieldtype"].ToString() : "";
                            switch (fieldtype)
                            {
                                case "date":
                                    fieldChange.Value = displayvalue != "" ? Convert.ToDateTime(displayvalue).ToShortDateString() : "";
                                    break;
                                default:
                                    fieldChange.Value = displayvalue;
                                    break;
                            }
                        }
                    }
                    else if (fieldChange.UseCurrentDate)
                    {
                        fieldChange.Value = detail.CompletedOn.HasValue ? detail.CompletedOn.Value.ToShortDateString() : "";
                    }
                    else
                    {
                        fieldChange.Value = field["@ValueLabel"] != null ? field["@ValueLabel"] : field["@Value"] != null ? field["@Value"] : "";
                    }

                    if (isFromActionForm && formFieldId != null && stepId != 0)
                    {
                        var fieldData = formFieldId.Trim().Split('|');
                        var actionFieldType = fieldData[0];
                        var actionFieldTypeId = int.Parse(fieldData[1]);

                        var actionField = Company.Fields.FirstOrDefault(x => x.FieldTypeID == actionFieldTypeId && x.ObjectID == detail.ObjectID);
                        if (fieldChange.Type == "Link")
                        {
                            fieldChange.Value = actionField?.Value;
                        }
                        else
                        {
                            fieldChange.Value = actionField?.FormattedValue;
                        }

                    }
                    else if (fieldChange.UseOutputValue == true && formFieldId != null && stepId != 0)
                    {
                        fieldChange.Value = Company.GetOutputFieldValue(stepId, detail.ItemID, formFieldId) ?? "";
                    }

                    fieldChanges.Add(fieldChange);
                }

            }

            return fieldChanges;
        }


        private void SetReassignObjectName(WorkflowStepDetail detail)
        {
            var ItemFields = detail.ItemFields;
            var userList = new List<int>();
            
            if (ItemFields != null && ItemFields.Reassigned != null)
            {
                for (int i = 0; i < detail.ItemFields.Reassigned.Count; i++)
                {
                    var reassigned = detail.ItemFields.Reassigned[i];

                    if (reassigned != null)
                    {
                        if (reassigned["@reassignType"] == "Object")
                        {
                            int objectId = (int)reassigned["@objectId"];
                            var objectType = reassigned["@objectType"];
                            var sql = @"Select A.Uid, D.DisplayValue as ObjectName
                        From
                        Asset A
                        cross apply dbo.GetAssetDisplayValueById(A.ID) D
                        where   A.Object = @obj and A.ObjectID = @objId";
                            var objectDetails = Company.Query<dynamic>(sql, new { obj = objectType.Value, objId = objectId }).FirstOrDefault();
                            reassigned["@objectName"] = objectDetails.ObjectName;
                            reassigned["@objectUid"] = objectDetails.Uid;

                            if (reassigned["@byResourceId"] != null)
                            {
                                userList.Add((int)reassigned["@byResourceId"]);
                            }

                            if (reassigned["@newIssueId"] != null)
                            {
                                var newWorkflowDetails = Company.Query<dynamic>(@"select i.Id, v.TypeId from workflow.item i 
                                    inner join workflow.version v on v.id = i.versionid where [object] = 'Issue' and objectid = @newIssueId"
                                    , new { newIssueId = (int)reassigned["@newIssueId"] }).FirstOrDefault();
                                if (newWorkflowDetails != null)
                                {
                                    reassigned["@newItemId"] = newWorkflowDetails.Id;
                                }
                            }
                        }
                        else if (reassigned["@reassignType"] == "Resource" && reassigned["@toResourceId"] != null)
                        {                            
                            userList.Add((int)reassigned["@toResourceId"]);

                            if (reassigned["@fromResourceId"] != null)
                            {
                                userList.Add((int)reassigned["@fromResourceId"]);
                            }
                            if (reassigned["@byResourceId"] != null)
                            {
                                userList.Add((int)reassigned["@byResourceId"]);
                            }
                        }
                        else if (reassigned["@reassignType"] == "Resource" && reassigned["@objectType"] != null)
                        {                            
                            userList.Add((int)reassigned["@toResourceId"]);

                            if (reassigned["@fromResourceId"] != null)
                            {
                                userList.Add((int)reassigned["@fromResourceId"]);
                            }
                            if (reassigned["@byResourceId"] != null)
                            {
                                userList.Add((int)reassigned["@byResourceId"]);
                            }
                        }
                    }
                }

                if (userList.Any())
                {
                    //get all the users at once
                    var users = Company.GlobalReportingResources.Where(r => userList.Contains(r.ResourceID)).ToList();
                    //apply names
                    for (int i = 0; i < detail.ItemFields.Reassigned.Count; i++)
                    {
                        var reassigned = detail.ItemFields.Reassigned[i];

                        if (reassigned["@reassignType"] == "Resource" && reassigned["@objectType"] == null)
                        {
                            if (reassigned["@byResourceId"] != null)
                            {
                                var res = users.FirstOrDefault(r => r.ResourceID == (int)reassigned["@byResourceId"]);
                                reassigned["@byResourceName"] = res == null ? "[unknown user]" : res.FullName;
                            }

                            if (reassigned["@toResourceId"] != null)
                            {
                                var res = users.FirstOrDefault(r => r.ResourceID == (int)reassigned["@toResourceId"]);
                                reassigned["@toResourceName"] = res == null ? "[unknown user]" : res.FullName;
                            }

                            if (reassigned["@fromResourceId"] != null)
                            {
                                var res = users.FirstOrDefault(r => r.ResourceID == (int)reassigned["@fromResourceId"]);
                                reassigned["@fromResourceName"] = res == null ? "[unknown user]" : res.FullName;
                            }
                        }
                        else
                        {
                            if (reassigned["@byResourceId"] != null)
                            {
                                var res = users.FirstOrDefault(r => r.ResourceID == (int)reassigned["@byResourceId"]);
                                reassigned["@byResourceName"] = res == null ? "[unknown user]" : res.FullName;
                            }

                            if (reassigned["@toResourceId"] != null)
                            {
                                var res = users.FirstOrDefault(r => r.ResourceID == (int)reassigned["@toResourceId"]);
                                reassigned["@toResourceName"] = res == null ? "[unknown user]" : res.FullName;
                            }

                            if (reassigned["@objectType"] != null)
                            {
                                int objectId = (int)reassigned["@objectId"];
                                var objectType = (string)reassigned["@objectType"] ?? "";
                                var previousObjectName = "";
                                if (objectType == "ResponsibilityType")
                                {
                                    var resp = Company.ResponsibilityTypes.Where(x => x.ID == objectId).FirstOrDefault();
                                    previousObjectName = resp != null ? (" - " + resp.Name) : "[unknown]";
                                }
                                else if (objectType == "Specific Users")
                                {
                                    previousObjectName = "";
                                }
                                else
                                {
                                    var sql = @"Select D.DisplayValue as ObjectName
                                                From
                                                Asset A
                                                cross apply dbo.GetAssetDisplayValueById(A.ID) D
                                                where   A.Object = @obj and A.ObjectID = @objId";
                                    previousObjectName = (" - ") + Company.Query<string>(sql, new { obj = objectType, objId = objectId }).FirstOrDefault();

                                }
                                reassigned["@fromResourceName"] = $"{objectType}{previousObjectName}";
                            }
                        }
                    }
                }
            }
        }

        [Route("step/detail/{itemStepId:int}"), HttpGet]
        public async Task<HttpResponseMessage> GetWorkflowVersionStepDetail(int itemStepId)
        {
            var itemStep = Company.WorkflowItemSteps.FirstOrDefault(i => i.ID == itemStepId);

            var sql = @"
	        select
				vs.ID,
                v.TypeID,
                si.StepID,
                si.ItemID,
                si.ID as ItemStepID,
	            vs.StepType,
	            vs.ActivityType,
	            vs.Settings as SettingsXml,
                vs.Fields as FieldsXml,
				si.Settings as ItemSettingsXml,
				si.Fields as ItemFieldsXml,
                si.StartedOn,
                si.CompletedOn,
                si.StartedBy,
                si.CompletedBy,
	            vs.[Name],
				e.ChangeType,
				e.[Object] as ObjectType,
				e.ObjectID as ObjectTypeID,
				ta.[Name] as ObjectTypeName,
				case when vs.StepType = 1 then
					dbo.GetWorkflowConditionLabels(e.Condition) 
				else
					null
				end as ConditionXml,
				e.Settings as EventSettingsXml,
				i.[Object],
				i.ObjectID,
				d.DisplayValue as ObjectName,
				case when e.Object = 'IssueType' then
					cast(1 as bit)
				else
					cast(0 as bit)
				end as IsIssueType,
				v.[Version],
				case when v.ID = t.PublishedVersionID then
					cast(1 as bit)
				else
					cast(0 as bit)
				end as IsPublishedVersion,
                a.id as AssetId
			from workflow.itemstep si
			inner join workflow.item i on i.id = si.itemid
			left join Asset a on a.[Object] = i.Object and a.ObjectID = i.ObjectID
			cross apply dbo.GetAssetDisplayValueById(a.id) d
			inner join workflow.versionstep vs on vs.id = si.stepid
			inner join workflow.version v on v.ID = vs.VersionID
			inner join workflow.[type] t on t.id = v.typeid
			inner join workflow.eventregistration e on e.TypeID = v.TypeID
			left join AssetType ta on ta.[Object] = e.[Object] and ta.ObjectID = e.ObjectID
			where si.ID = @itemStepId";

            var detail = Company.Query<WorkflowStepDetail>(sql, new { itemStepId }).FirstOrDefault();

            if (detail == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception(WorkflowApiMessages.StepNotFound));

            //TODO: refactor to convert to directly to a model instead of xml to json to dynamic
            detail.Settings = XmlToDynamic(detail.SettingsXml);
            detail.Fields = XmlToDynamic(detail.FieldsXml);
            detail.ItemSettings = XmlToDynamic(detail.ItemSettingsXml);
            detail.Condition = XmlToDynamic(detail.ConditionXml);
            detail.EventSettings = XmlToDynamic(detail.EventSettingsXml);
            detail.ItemFields = XmlToDynamic(detail.ItemFieldsXml);

            if (detail.ItemSettings == null)
                detail.ItemSettings = new JObject();
            detail.ItemSettings.hasEmails = false;
            detail.ItemSettings.hasForms = false;
            detail.ItemSettings.hasConditions = false;

            try
            {
                detail.FieldChanges = this.GetWorkFlowStepFieldChanges(detail);
                detail.RelationshipChange = this.GetWorkFlowStepRelationshipChanges(detail.Settings, detail.ItemID, detail.ObjectName);

                var itemFields = (WorkflowItemStepDetail.FieldsModel)new XmlSerializer(typeof(WorkflowItemStepDetail.FieldsModel)).Deserialize(new StringReader(detail.ItemFieldsXml));

                if (detail.Settings != null && detail.Settings.State != null && !string.IsNullOrEmpty(detail.Settings.State.Value))
                    detail.StateChange = (State)Convert.ToInt32(detail.Settings.State.Value);

                string issueObject = null;
                int issueObjectId = 0;
                if (detail.Condition != null && detail.Condition.Condition != null)
                {
                    detail.Condition = detail.Condition.Condition;
                    if (detail.Condition.GetType() != typeof(JArray))
                        detail.Condition = new JArray(detail.Condition);
                    for (int i = 0; i < detail.Condition.Count; i++)
                    {
                        var condition = detail.Condition[i];
                        if (condition["@ContextualFieldID"] != null)
                        {
                            var fieldId = condition["@ContextualFieldID"].Value;
                            switch (fieldId)
                            {
                                case "IssueObject":
                                    issueObject = condition["@Value"].Value;
                                    break;
                                case "IssueObjectID":
                                    int.TryParse(condition["@Value"].Value, out issueObjectId);
                                    break;
                            }
                        }
                        else
                            detail.ItemSettings.hasConditions = true;
                    }
                }

                var assetId = detail.AssetId;
                if (detail.IsIssueType)
                {
                    var issueSql = @"select 
				        I.ID,
				        S.ID as IssueID,
				        T.ID as IssueTypeID,
				        T.[Name] as IssueName,
				        D.DisplayValue as ObjectName,
			            TA.[Name] as ObjectTypeName,
				        A.[Object],
				        A.ObjectID,
				        TA.[Object] as ObjectType,
				        TA.ObjectID as ObjectTypeID,
                        A.ID as AssetId
			        from workflow.item i
			        inner join Issue s on s.ID = i.ObjectID
			        inner join IssueType t on t.id = s.IssueTypeID
			        inner join Asset A on A.Object = s.Object and A.ObjectID = s.ObjectID
			        inner join AssetType TA on TA.ID = A.AssetTypeID
			        cross apply dbo.GetAssetDisplayValueById(A.ID) D
			         where i.ID = @itemId";

                    var issueDetails = Company.Query<WorkflowStepIssueDetail>(issueSql, new { itemId = detail.ItemID }).FirstOrDefault();
                    if (issueDetails != null)
                    {
                        detail.IssueDetails = issueDetails;
                        assetId = issueDetails.AssetId;
                    }

                }

                //deal with xml to json nonsense and load detail values
                if (detail.ActivityType == WorkflowActivityType.EmailNotification || detail.ActivityType == WorkflowActivityType.Form)
                {
                    var eventInfo = new EventObjectInfo()
                    {
                        Object = (SystemObjects)Enum.Parse(typeof(SystemObjects), detail.Object),
                        ObjectID = detail.ObjectID,
                        ObjectType = (SystemObjects)Enum.Parse(typeof(SystemObjects), detail.ObjectType),
                        ObjectTypeID = detail.ObjectTypeID,
                    };

                    List<int> responsiblities = new List<int>();
                    if (detail.Settings != null)
                    {
                        if (detail.Settings.MessageSubjectTemplate != null)
                            detail.Settings.MessageSubjectTemplate = await Company.ProcessMessageTokens(detail.Settings.MessageSubjectTemplate.Value, eventInfo, Company.CurrentCompanyDomain, itemStep);
                        else
                            detail.Settings.MessageSubjectTemplate = string.Empty;

                        if (detail.Settings.MessageBodyTemplate != null)
                            detail.Settings.MessageBodyTemplate = await Company.ProcessMessageTokens(detail.Settings.MessageBodyTemplate.Value, eventInfo, Company.CurrentCompanyDomain, itemStep);

                        if (detail.Settings.IncludePreviousFormResponses != null && detail.Settings.IncludePreviousFormResponses == "true")
                            detail.Settings.MessageBodyTemplate += Company.GenerateFormResponsesEmailContent(itemStep.ItemID);

                        if (detail.Settings.MessageRecipientType == EmailTaskRecipientType.Responsibility)
                        {
                            detail.ItemSettings.Responsibilities = new JObject();
                            List<dynamic> responsibilitiesList = new List<dynamic>();


                            if (detail.Settings.ResponsibilityTypeID.GetType() != typeof(JArray))
                            {
                                detail.Settings.ResponsibilityTypeID = new JArray(detail.Settings.ResponsibilityTypeID);
                            }

                            for (int i = 0; i < detail.Settings.ResponsibilityTypeID.Count; i++)
                            {
                                var resId = detail.Settings.ResponsibilityTypeID[i].Value;
                                if (int.TryParse(resId, out int resIdInt))
                                {
                                    var resp = Company.GetById<ResponsibilityType>(resIdInt);
                                    if (resp != null)
                                    {
                                        responsiblities.Add(resp.ID);
                                        responsibilitiesList.Add(new { id = resp.ID, name = resp.Name });
                                    }
                                }
                            }

                            detail.ItemSettings.Responsibilities = JToken.FromObject(responsibilitiesList);
                        }

                    }

                    if (detail?.Fields?.form != null)
                    {
                        if (detail.Fields.form["@description"] != null)
                            detail.Fields.form["@description"] = await Company.ProcessMessageTokens(detail.Fields.form["@description"].Value, eventInfo, Company.CurrentCompanyDomain, itemStep);
                    }

                    if (detail?.ItemSettings?.emails != null)
                    {
                        var emails = detail.ItemSettings.emails;

                        if (emails.email != null)
                        {
                            detail.ItemSettings.hasEmails = true;
                            if (emails.email.GetType() != typeof(JArray))
                            {
                                emails.email = new JArray(emails.email);
                            }
                        }
                        else
                        {
                            detail.ItemSettings.emails.email = new JArray();
                        }

                        detail.ItemSettings.hasEmails = (emails.email.Count > 0);

                        var resourceEmails = new List<string>();

                        for (int i = 0; i < emails.email.Count; i++)
                        {
                            var e = emails.email[i];
                            string address = e["@address"].Value;

                            if (!resourceEmails.Any(r => r == address.ToLower()))
                                resourceEmails.Add(address.ToLower());
                        }

                        //get all relevant resource info
                        var emailResources = this.GetEmailResources(assetId, responsiblities, resourceEmails);
                        for (int i = 0; i < emails.email.Count; i++)
                        {
                            var e = emails.email[i];
                            string address = e["@address"].Value;

                            var res = emailResources.FirstOrDefault(r => r.Email.ToLower() == address.ToLower());
                            if (res != null)
                            {
                                e.name = res.FullName;
                                e.id = res.ResourceID;
                                e.responsibility = res.Responsibility;
                            }
                            else
                            {
                                e.name = (string)null;
                                e.id = 0;
                                e.responsibility = (string)null;
                            }
                        }

                    }
                    else
                    {
                        detail.ItemSettings.emails = new JObject();
                        detail.ItemSettings.emails.email = new JArray();
                    }

                    if (detail.ItemFields == null)
                    {
                        detail.ItemFields = new JObject();
                        detail.ItemFields.form = new JArray();
                    }

                    if (detail.ItemFields.form != null)
                    {
                        if (detail.ItemFields.form.GetType() != typeof(JArray))
                        {
                            detail.ItemFields.form = new JArray(detail.ItemFields.form);
                        }
                    }
                    else
                    {
                        detail.ItemFields.form = new JArray();
                    }

                    if (detail.ItemFields.Reassigned != null)
                    {
                        if (detail.ItemFields.Reassigned.GetType() != typeof(JArray))
                        {
                            detail.ItemFields.Reassigned = new JArray(detail.ItemFields.Reassigned);
                        }
                    }
                    else
                    {
                        detail.ItemFields.Reassigned = new JArray();
                    }

                    detail.ItemSettings.hasForms = itemFields.Forms.Any();
                    detail.ItemSettings.hasPendingForms = false;

                    if (itemFields.TotalResources > 0)
                    {
                        int total = itemFields.TotalResources;
                        int numResponses = itemFields.NumberOfResponses;

                        if (detail.ItemSettings.hasForms == false)
                        {
                            detail.ItemSettings.hasPendingForms = true;
                        }
                        else if (detail.Settings.FormResponseType == FormResponseType.FirstResponse)
                        {
                            detail.ItemSettings.hasPendingForms = detail.ItemSettings.hasForms == true ? false : true;
                        }
                        else if (detail.Settings.FormResponseType == FormResponseType.Majority && total > 0)
                        {
                            detail.ItemSettings.hasPendingForms = ((numResponses / (double)total) <= 0.5);
                        }
                        else if (detail.Settings.FormResponseType == FormResponseType.All)
                        {
                            detail.ItemSettings.hasPendingForms = (numResponses != total);
                        }
                    }

                    SetReassignObjectName(detail);

                    switch (detail.ActivityType)
                    {
                        case WorkflowActivityType.EmailNotification:
                        case WorkflowActivityType.Form:
                            detail.ItemSettings.hasEmails = false;
                            if (detail.ItemSettings.emails != null)
                            {
                                var emails = detail.ItemSettings.emails;

                                if (emails.email != null)
                                {
                                    detail.ItemSettings.hasEmails = true;
                                    if (emails.email.GetType() != typeof(JArray))
                                    {
                                        emails.email = new JArray(emails.email);
                                    }
                                }
                                else
                                {
                                    detail.ItemSettings.emails.email = new JArray();
                                }

                                detail.ItemSettings.hasEmails = (emails.email.Count > 0);

                                for (int i = 0; i < emails.email.Count; i++)
                                {
                                    var e = emails.email[i];
                                    string address = e["@address"].Value;
                                    var res = Company.GlobalReportingResources.FirstOrDefault(r => r.Email.ToLower() == address.ToLower());
                                    if (res != null)
                                    {
                                        e.name = res.FullName;
                                        e.id = res.ResourceID;
                                    }
                                    else
                                    {
                                        e.name = (string)null;
                                        e.id = 0;
                                    }
                                }
                            }
                            else
                            {
                                detail.ItemSettings.emails = new { email = new JArray() };
                            }
                            break;
                    }

                    var resourceIds = new List<int>();

                    if (detail.ActivityType == WorkflowActivityType.Form)
                    {
                        List<GlobalReportingResource> users = new List<GlobalReportingResource>();


                        if (detail.CompletedOn == null)
                        {
                            if (detail.Settings.MessageRecipientType == "SpecificUser")
                            {
                                users = new List<GlobalReportingResource>();
                                foreach (var email in ((string)detail.Settings.MessageToUser).Split(';'))
                                {
                                    var user = Company.GlobalReportingResources.FirstOrDefault(g => g.Email.ToLower() == email);
                                    if (user != null)
                                        users.Add(user);
                                }
                            }
                            else
                            {
                                users = Company.Query<GlobalReportingResource>(@"
                                select  distinct R.* 
                                from    reporting.Global_Resource R 
                                        inner join workflow.ItemAssignment IA on IA.ResourceObjectID = R.ResourceID and IA.ResourceObject = 'Resource'
                                where   IA.ItemID = @ItemID and (IA.ItemStepID = @ItemStepID or IA.ItemStepID is null)"
                                , new { detail.ItemID, detail.ItemStepID })
                                .ToList();
                            }

                            foreach (var res in itemFields.Reassignments)
                            {
                                if (res.ByResourceID == 0)
                                {
                                    continue;
                                }

                                var ix = users.FindIndex(u => u.ResourceID == res.FromResourceID);
                                if (ix > -1) users.RemoveAt(ix);

                                var dx = users.FindIndex(u => u.ResourceID == res.ToResourceID);
                                if (dx == -1) 
                                {
                                    var assignee = Company.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == res.ToResourceID);
                                    if (assignee != null)
                                    {
                                        users.Add(assignee);
                                    }
                                }
                            }

                            var userHasOpenAssignment = Company.WorkflowItemAssignments.Any(i => i.ItemID == detail.ItemID && (i.ItemStepID == detail.ItemStepID || i.ItemStepID == null) && i.ResourceObject == "Resource"
                                && i.ResourceObjectID == Company.CurrentResourceID);
                            detail.AssignedUsers = users;
                            detail.IsAssignedLoginUser = userHasOpenAssignment && users.Any(x => x.ResourceID == Company.CurrentResourceID);
                        }
                    }

                        foreach (var form in itemFields.Forms)
                        {
                            if (form.ResourceID != 0 && !resourceIds.Any(r => r == form.ResourceID))
                                resourceIds.Add(form.ResourceID);
                        }

                        for (int i = 0; i < detail.ItemFields.form.Count; i++)
                        {
                            var form = detail.ItemFields.form[i];

                            if (form.field != null)
                            {
                                if (form.field.GetType() != typeof(JArray))
                                {
                                    form.field = new JArray(form.field);
                                }
                            }
                            else
                            {
                                form.field = new JArray();
                            }

                            if (form["@ResourceID"] != null & form["@ResourceID"].Value != null & int.TryParse(form["@ResourceID"].Value, out int resId))
                            {
                                if (!resourceIds.Any(r => r == resId))
                                    resourceIds.Add(resId);
                            }
                        }


                        //get all relevant resource info
                        var formResources = Company.GlobalReportingResources.Where(r => resourceIds.Contains(r.ResourceID)).ToList();

                        for (int i = 0; i < detail.ItemFields.form.Count; i++)
                        {
                            var form = detail.ItemFields.form[i];

                            if (form["@ResourceID"] != null & form["@ResourceID"].Value != null & int.TryParse(form["@ResourceID"].Value, out int resId))
                            {
                                var res = formResources.FirstOrDefault(r => r.ResourceID == resId);
                                if (res != null)
                                    form.resourceName = res.FullName;
                            }
                            SetWorkFlowStepRelationshipAssets(form);
                        }
                    }
                }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }

            return Request.CreateResponse(HttpStatusCode.OK, detail);
        }

        [Route("item/{itemId:int}/excel/excel.xls"), HttpGet]
        public HttpResponseMessage GetItemStepsExcel(int itemId)
        {
            var item = Company.WorkflowItems.FirstOrDefault(i => i.ID == itemId);

            if (item == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, new Exception(WorkflowApiMessages.ItemNotFound));

            var results = Company.Query<dynamic>(QueryConstants.WorkflowItemSteps, new { itemId }).ToList();

            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Steps");

            #region Create the list sheet

            #region Header

            var colIndex = 0;

            document.SetCellValue(1, ++colIndex, "Step Name");
            document.SetCellValue(1, ++colIndex, "Step Type");
            document.SetCellValue(1, ++colIndex, "Complete");
            document.SetCellValue(1, ++colIndex, "Activity Type");
            document.SetCellValue(1, ++colIndex, "Assignee");
            document.SetCellValue(1, ++colIndex, "Date Started");
            document.SetCellValue(1, ++colIndex, "Date Completed");
            document.SetCellValue(1, ++colIndex, "Workflow Step UID");

            #endregion

            int rowIndex = 1;
            foreach (var row in results)
            {
                var dataColIndex = 0;
                rowIndex++;

                var activityType = row.ActivityType != null ? ((WorkflowActivityType)Enum.ToObject(typeof(WorkflowActivityType), row.ActivityType)).GetName() : "";

                document.SetCellValue(rowIndex, ++dataColIndex, row.Name ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.StepType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Complete ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, activityType ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.Assignee ?? "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.StartedOn != null ? row.StartedOn.ToShortDateString() : "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.CompletedOn != null ? row.CompletedOn.ToShortDateString() : "");
                document.SetCellValue(rowIndex, ++dataColIndex, row.UID.ToString() ?? "");
            }

            #endregion


            var stream = new MemoryStream();
            document.SaveAs(stream);
            stream.Position = 0;
            HttpResponseMessage result = null;
            // serve the file to the client      
            result = Request.CreateResponse(HttpStatusCode.OK);

            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = $"Workflow steps {DateTime.Now.ToShortDateString()}.xlsx"
            };
            return result;
        }

        [Route("resources"), HttpGet]
        public HttpResponseMessage GetResources()
        {
            var queryParams = Request.GetQueryStrings();
            string gbFilter = "";
            string excludedResourceId = "-1";
            queryParams.TryGetValue("gbfilter", out gbFilter);
            queryParams.TryGetValue("excludedResourceId", out excludedResourceId);

            var innerSql = $@"select FirstName + ' ' + LastName as [Text],  'Resource|' + cast(ResourceID as varchar) + '|' + FirstName + ' ' + LastName as [Value] from reporting.Global_resource where [State] = 1 and ResourceID <> @excludedResourceId";
            var filter = "";
            var dbArgs = new Dapper.DynamicParameters();

            if (!string.IsNullOrEmpty(gbFilter))
            {
                filter = " and [Text] like '%' + @gbfilter + '%'";
                dbArgs.Add("gbfilter", gbFilter);
            }
            dbArgs.Add("excludedResourceId", excludedResourceId);

            var pagingSuffix = applyPagingSuffix("", Request);

            var sql = $@"select * from ({innerSql}) users where 1=1 {filter} order by [Text] asc {pagingSuffix}";
            var countSql = $@"select count(1) from ({innerSql}) users where 1=1 {filter}";


            var total = Company.Query<int>(countSql, dbArgs).First();
            var results = Company.Query<dynamic>(sql, dbArgs);

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                total,
                results
            });
        }

        [Route("ReassignWorkflowResource/bulk")]
        public async Task<HttpResponseMessage> BulkReassignForm(BulkWorkflowReassignModel model)
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Forbidden, WorkflowApiMessages.NoPermissionBulkReassign);

            if (model == null || model.ItemStepIDs == null || model.ItemStepIDs.Count < 1)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ApiMessages.ErrorInvalidDatasetMessage);
            }

            var resource = Company.GlobalReportingResources.FirstOrDefault(r => r.ResourceID == model.NewAssigneeResourceID);

            if (model.NewAssigneeResourceID < 0 || resource == null)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, WorkflowApiMessages.InvalidResourceID);

            var itemSteps = Company.WorkflowItemSteps.Where(x => model.ItemStepIDs.Contains(x.ID)).Include(x => x.Item).Include(x => x.Step).ToList();

            try
            {
                await Company.BulkWorkflowFormReassign(itemSteps, resource, model.OriginalAssigneeResourceID, model.SendFormEmails, model.ClearOtherAssignments);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }

            return Request.CreateResponse(HttpStatusCode.OK, new { type =ApiMessages.Success, message = WorkflowApiMessages.WorkflowReassignSuccess, title = ApiMessages.Success });
        }

        #region Helper Methods

        private string GetConditionLabels(string conditions)
        {
            return Company.Query<string>("select dbo.GetWorkflowConditionLabels(@conditions)", new { conditions }).FirstOrDefault();
        }

        private dynamic XmlToDynamic(string xml, bool omitRootElement = true)
        {
            return XmlToObject<dynamic>(xml, omitRootElement);
        }

        private T XmlToObject<T>(string xml, bool omitRootElement = true)
        {
            return string.IsNullOrEmpty(xml) ? JsonConvert.DeserializeObject<T>("{}") : JsonConvert.DeserializeObject<T>(JsonConvert.SerializeXNode(XElement.Parse(xml), Formatting.None, omitRootElement));
        }

        /// <summary>
        /// Maps condition properties from a JSON string referencing temporary id's to the actual id's created
        /// </summary>
        private string MapWorkflowConditions(string conditionString, Dictionary<int, int> mappings)
        {
            if (!string.IsNullOrEmpty(conditionString))
            {
                dynamic condition = JsonConvert.DeserializeObject(conditionString);

                if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count > 0)
                {
                    for (int i = 0; i < condition.Conditions.Condition.Count; i++)
                    {
                        var c = condition.Conditions.Condition[i];

                        if (c["@VersionStepID"] != null && mappings.ContainsKey((int)c["@VersionStepID"]))
                        {
                            condition.Conditions.Condition[i]["@VersionStepID"] = mappings[(int)c["@VersionStepID"]];
                        }
                    }
                    return JsonConvert.SerializeObject(condition);
                }
                else if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count == 0)
                {
                    condition.Conditions = null;
                    return JsonConvert.SerializeObject(condition);
                }
                else
                {
                    return JsonConvert.SerializeObject(condition);
                }
            }

            return null;
        }


        private string MapWorkflowConditionsFromXml(string condtionXml, Dictionary<int, int> mappings)
        {

            if (!string.IsNullOrEmpty(condtionXml))
            {
                XElement root = XElement.Parse(condtionXml);

                IEnumerable<XElement> conditions =
                                from el in root.Elements("Condition")
                                where el.Attribute("VersionStepID") != null
                                select el;

                foreach (XElement el in conditions)
                {
                    if (mappings.ContainsKey((int)el.Attribute("VersionStepID")))
                    {
                        el.Attribute("VersionStepID").SetValue(mappings[(int)el.Attribute("VersionStepID")]);
                    }

                }
                return root.ToString();
            }
            return condtionXml;


        }

        /// <summary>
        /// Maps settings properties from an XML string referencing temporary id's to the actual id's created
        /// </summary>
        private string MapWorkflowFieldSettings(string settingsString, Dictionary<int, int> mappings)
        {
            if (!string.IsNullOrEmpty(settingsString))
            {
                dynamic settings = XmlToDynamic(settingsString);

                if (settings.FieldUpdate != null && settings.FieldUpdate.Field != null)
                {
                    var fields = settings.FieldUpdate.Field;
                    var count = fields.Count == null ? 1 : fields.Count;


                    if (fields.Count == null)
                    {
                        var field = fields;
                        var shouldUpdate = (field["@UseFormValue"] != null && field["@UseFormValue"].ToString().ToLower() == "true") || (field["@UseOutputValue"] != null && field["@UseOutputValue"].ToString().ToLower() == "true");
                        if (shouldUpdate && mappings.ContainsKey((int)field["@FormStepId"]))
                            field["@FormStepId"] = mappings[(int)field["@FormStepId"]];
                    }
                    else
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var field = fields[i];
                            var shouldUpdate = (field["@UseFormValue"] != null && field["@UseFormValue"].ToString().ToLower() == "true") || (field["@UseOutputValue"] != null && field["@UseOutputValue"].ToString().ToLower() == "true");

                            if (shouldUpdate && mappings.ContainsKey((int)field["@FormStepId"]))
                                field["@FormStepId"] = mappings[(int)field["@FormStepId"]];
                        }
                    }

                }

                return JsonConvert.DeserializeXNode(JsonConvert.SerializeObject(new { settings = settings })).ToString();
            }

            return settingsString;
        }

        private string MapWorkflowRelationshipUpdateSettings(string settingsString, Dictionary<int, int> mappings)
        {
            if (!string.IsNullOrEmpty(settingsString))
            {
                dynamic settings = XmlToDynamic(settingsString);

                if (settings.RelationshipUpdate != null && settings.RelationshipUpdate.Relationship != null)
                {
                    var relationship = settings.RelationshipUpdate.Relationship;

                    if (relationship["@FormStepId"] != null)
                        relationship["@FormStepId"] = mappings[(int)relationship["@FormStepId"]];

                }

                return JsonConvert.DeserializeXNode(JsonConvert.SerializeObject(new { settings = settings })).ToString();
            }

            return settingsString;
        }

        private void MapWorkflowHttpSettings(WorkflowVersionStep node, int key, string field, Dictionary<int, int> keyMapping)
        {
            var parts = field.ToString().Split('|');
            int httpKey = 0;
            int.TryParse(parts[1], out httpKey);

            if (key != 0 && httpKey != 0)
            {
                if (keyMapping.ContainsKey(httpKey))
                    httpKey = keyMapping[httpKey];

                node.Settings = node.Settings.Replace(field.ToString(), $"[HTTPREQUEST|{httpKey}|{parts[2]}");
            }
        }

        private void MapWorkflowHttpResponseTokens(WorkflowVersionStep node, int key, string field, Dictionary<int, int> keyMapping)
        {
            var parts = field.ToString().Split('|');
            int httpKey = 0;
            int.TryParse(parts[1], out httpKey);

            if (key != 0 && httpKey != 0)
            {
                if (keyMapping.ContainsKey(httpKey))
                    httpKey = keyMapping[httpKey];

                node.Settings = node.Settings.Replace(field.ToString(), $"[HTTPRESPONSE|{httpKey}|{parts[2]}");
            }
        }


        private string MapWorkflowHttpResponseSettings(string settingsString, Dictionary<int, int> keyMapping)
        {
            dynamic settings = XmlToDynamic(settingsString);

            if (settings.HTTPResponse != null)
            {
                if (settings.HTTPResponse.InputStepId != null)
                {
                    if (int.TryParse(settings.HTTPResponse.InputStepId.ToString(), out int inputId) && keyMapping.ContainsKey(inputId))
                    {
                        settings.HTTPResponse.InputStepId = keyMapping[inputId].ToString();
                    }
                }

                if (settings.HTTPResponse.Outputs != null)
                {
                    if (settings.HTTPResponse.Outputs.Count == null)
                    {
                        var output = settings.HTTPResponse.Outputs;
                        if (int.TryParse(output.StepId.ToString(), out int stepId) && keyMapping.ContainsKey(stepId))
                        {
                            output.StepId = keyMapping[stepId].ToString();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < settings.HTTPResponse.Outputs.Count; i++)
                        {
                            var output = settings.HTTPResponse.Outputs[i];
                            if (int.TryParse(output.StepId.ToString(), out int stepId) && keyMapping.ContainsKey(stepId))
                            {
                                output.StepId = keyMapping[stepId].ToString();
                            }
                        }
                    }
                }

                return JsonConvert.DeserializeXNode(JsonConvert.SerializeObject(new { settings = settings })).ToString();
            }

            return settingsString;
        }

        private void MapNodeSettingsAndTokens(WorkflowDiagramNode n, Dictionary<int,int> keyMapping)
        {
            List<WorkflowActivityType> tokenTypes = new List<WorkflowActivityType>()
                    {
                        WorkflowActivityType.Form,
                        WorkflowActivityType.EmailNotification,
                        WorkflowActivityType.HTTPRequest
                    };

            if (n.ActivityType == WorkflowActivityType.FieldChange)
            {

                int key;
                if (!int.TryParse(n.Key, out key)) return;
                if (keyMapping.ContainsKey(key))
                    key = keyMapping[key];

                var node = Company.GetById<WorkflowVersionStep>(key);
                node.Settings = MapWorkflowFieldSettings(node.Settings, keyMapping);

            }
            if (n.ActivityType == WorkflowActivityType.RelationshipChange)
            {
                int key;
                if (!int.TryParse(n.Key, out key)) return;
                if (keyMapping.ContainsKey(key))
                    key = keyMapping[key];

                var node = Company.GetById<WorkflowVersionStep>(key);
                node.Settings = MapWorkflowRelationshipUpdateSettings(node.Settings, keyMapping);

            }
            if (n.ActivityType == WorkflowActivityType.HTTPResponse)
            {
                int key;
                if (!int.TryParse(n.Key, out key)) return;
                if (keyMapping.ContainsKey(key))
                    key = keyMapping[key];

                var node = Company.GetById<WorkflowVersionStep>(key);
                node.Settings = MapWorkflowHttpResponseSettings(node.Settings, keyMapping);

            }
            if (tokenTypes.Contains(n.ActivityType))
            {
                var fields = Regex.Matches(n.Settings, "\\[HTTPREQUEST\\|(-?)([0-9.]+)\\|([a-zA-Z]+)\\]");

                foreach (var field in fields)
                {
                    int key;
                    if (!int.TryParse(n.Key, out key)) return;
                    if (keyMapping.ContainsKey(key))
                        key = keyMapping[key];

                    var node = Company.GetById<WorkflowVersionStep>(key);
                    MapWorkflowHttpSettings(node, key, field.ToString(), keyMapping);
                }

                fields = Regex.Matches(n.Settings, "\\[HTTPRESPONSE\\|(-?)([0-9.]+)\\|([0-9.]+)\\]");

                foreach (var field in fields)
                {
                    int key;
                    if (!int.TryParse(n.Key, out key)) return;
                    if (keyMapping.ContainsKey(key))
                        key = keyMapping[key];

                    var node = Company.GetById<WorkflowVersionStep>(key);
                    MapWorkflowHttpResponseTokens(node, key, field.ToString(), keyMapping);
                }
            }
        }

        #endregion
    }
}