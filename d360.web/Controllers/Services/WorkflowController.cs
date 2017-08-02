using System;
using d360.model;
using System.Net.Http;
using System.Web.Http;
using System.Linq;
using System.Runtime.Serialization;
using d360.core;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Xml.Linq;
using d360.core.entities;
using System.Net;
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
using System.Collections;
using System.Text.RegularExpressions;
using d360.web.Models.Attributes;

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
               
        
        [Route("all/issues"), HttpGet]
        public HttpResponseMessage GetIssuesForAllUsers()
        {
            var sql = @"
              select distinct
                wi.WorkflowID
                ,wi.WorkflowItemID
	            ,c.Body
	            ,wi.DateStarted
	            ,wi.DateCompleted
	            ,wi.IsCompleted
	            ,wi.Name
	            ,wi.Object
	            ,cast(coalesce(wr.ResourceID, 0) as bit) as AllowAction
	            ,wi.RaisedBy
	            ,wi.ObjectID
	            ,wi.CreatingResourceID as RaisedByResourceID
	            ,wi.Url
	            ,case wi.IsCompleted
                    when 1 then 'Closed'
		            else
			            case cast(coalesce(wr.ResourceID, 0) as bit)

                            when 1 then 'Pending'
				            else 'Waiting on user(s)'

                        end

                end as ActivityName
	            ,wi.Notes
	            ,wi.Comments
	            ,wi.IssueType
	            ,wi.IssueTypeName
	            ,wi.IssueID
	            ,wi.CriticalityName as Criticality
	            ,wi.EllapsedDays
            from

                WorkflowIssue wi

                left outer join Comment c on wi.CommentID = c.ID

                left outer join WorkflowResource wr on (wr.WorkflowID = wi.WorkflowID and wr.ResourceID = @r and wr.IsComplete = 0)
            order by DateStarted desc";

            var list = Company.Query<dynamic>(sql, new { r = Company.CurrentResourceID });

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }


        [Route("my/issues"), HttpGet]
        public HttpResponseMessage GetIssuesForMyUser()
        {
            var sql = @"
              select distinct
                wi.WorkflowID
                ,wi.WorkflowItemID
	            ,c.Body
	            ,wi.DateStarted
	            ,wi.DateCompleted
	            ,wi.IsCompleted
	            ,wi.Name
	            ,wi.Object
	            ,cast(coalesce(wr.ResourceID, 0) as bit) as AllowAction
	            ,wi.RaisedBy
	            ,wi.ObjectID
	            ,wi.CreatingResourceID as RaisedByResourceID
	            ,wi.Url
	            ,case wi.IsCompleted
                    when 1 then 'Closed'
		            else
			            case cast(coalesce(wr.ResourceID, 0) as bit)

                            when 1 then 'Pending'
				            else 'Waiting on user(s)'

                        end

                end as ActivityName
	            ,wi.Notes
	            ,wi.Comments
	            ,wi.IssueType
	            ,wi.IssueTypeName
	            ,wi.IssueID
	            ,wi.CriticalityName as Criticality
	            ,wi.EllapsedDays
            from
                WorkflowIssue wi
                left outer join Comment c on wi.CommentID = c.ID
                inner join WorkflowResource wr on (wr.WorkflowID = wi.WorkflowID and wr.ResourceID = @r and wr.IsComplete = 0)
            order by DateStarted desc";

            var list = Company.Query<dynamic>(sql, new { r = Company.CurrentResourceID });

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }

        [Route("all/issues/excel/excel.xls"), HttpGet]
        public HttpResponseMessage GetIssuesForAllUsersExcel(bool all = true)
        {            
            var sql = "";
            if (all)
            {
                sql = @"
                      select distinct
                        wi.WorkflowID
                        ,wi.WorkflowItemID
	                    ,c.Body
	                    ,wi.DateStarted
	                    ,wi.DateCompleted
	                    ,wi.IsCompleted
	                    ,wi.Name
	                    ,wi.Object
	                    ,cast(coalesce(wr.ResourceID, 0) as bit) as AllowAction
	                    ,wi.RaisedBy
	                    ,wi.ObjectID
	                    ,wi.CreatingResourceID as RaisedByResourceID
	                    ,wi.Url
	                    ,case wi.IsCompleted
                            when 1 then 'Closed'
		                    else
			                    case cast(coalesce(wr.ResourceID, 0) as bit)

                                    when 1 then 'Pending'
				                    else 'Waiting on user(s)'

                                end

                        end as ActivityName
	                    ,wi.Notes
	                    ,wi.Comments
	                    ,wi.IssueType
	                    ,wi.IssueTypeName
	                    ,wi.IssueID
	                    ,wi.CriticalityName as Criticality
	                    ,wi.EllapsedDays
                    from

                        WorkflowIssue wi

                        left outer join Comment c on wi.CommentID = c.ID

                        left outer join WorkflowResource wr on (wr.WorkflowID = wi.WorkflowID and wr.ResourceID = @r and wr.IsComplete = 0)
                    order by DateStarted desc";
            }
            else
            {
                sql = @"
                      select distinct
                        wi.WorkflowID
                        ,wi.WorkflowItemID
	                    ,c.Body
	                    ,wi.DateStarted
	                    ,wi.DateCompleted
	                    ,wi.IsCompleted
	                    ,wi.Name
	                    ,wi.Object
	                    ,cast(coalesce(wr.ResourceID, 0) as bit) as AllowAction
	                    ,wi.RaisedBy
	                    ,wi.ObjectID
	                    ,wi.CreatingResourceID as RaisedByResourceID
	                    ,wi.Url
	                    ,case wi.IsCompleted
                            when 1 then 'Closed'
		                    else
			                    case cast(coalesce(wr.ResourceID, 0) as bit)

                                    when 1 then 'Pending'
				                    else 'Waiting on user(s)'

                                end

                        end as ActivityName
	                    ,wi.Notes
	                    ,wi.Comments
	                    ,wi.IssueType
	                    ,wi.IssueTypeName
	                    ,wi.IssueID
	                    ,wi.CriticalityName as Criticality
	                    ,wi.EllapsedDays
                    from

                        WorkflowIssue wi

                        left outer join Comment c on wi.CommentID = c.ID
                        left outer join WorkflowResource wr on (wr.WorkflowID = wi.WorkflowID and (wr.ResourceID = @r or wi.CreatingResourceID = @r) and wr.IsComplete = 0)
                    order by DateStarted desc";                     
            }

            var results = Company.Query<dynamic>(sql, new { r = Company.CurrentResourceID });
            
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
        

        [Route("issue/type/{objectid:int}/{objecttype}"), HttpGet]
        public HttpResponseMessage GetTaskByIDForObjectAndType(int objectid, string objecttype) 
        {
            var sql = @"
              select distinct
                wi.WorkflowID
                ,wi.WorkflowItemID
	            ,c.Body as Issue
	            ,wi.DateStarted	            	            
	            ,wi.Name	as ObjectName            
	            ,wi.CreatingResourceID as ResourceID
				,wi.RaisedBy as ResourceName
	            ,dbo.GenerateObjectUrl('Resource', 0, wi.CreatingResourceID) as ResourceUrl           
	            ,wi.IssueType
	            ,wi.IssueTypeName
	            ,wi.IssueID
	            ,wi.CriticalityName
	            ,wi.EllapsedDays
				,wi.[Object]
				,wi.[ObjectID]
                ,wr.Activity
				,case wi.IsCompleted
                            when 1 then 'Closed'
		                    else
			                    case cast(coalesce(wr.ResourceID, 0) as bit)

                                    when 1 then 'Pending'
				                    else 'Waiting on user(s)'

                                end

                        end as ActivityName
            from

                WorkflowIssue wi				
                left outer join Comment c on wi.CommentID = c.ID                
				left outer join WorkflowResource wr on (wr.WorkflowID = wi.WorkflowID and wr.ResourceID = @r and wr.IsComplete = 0)
			where 
				wi.[object] = @obj and wi.[objectid] = @id and wi.iscompleted = 0
            order by DateStarted desc";

            var list = Company.Query<dynamic>(sql, new { r = Company.CurrentResourceID, id = objectid, obj = objecttype });

            return Request.CreateResponse(HttpStatusCode.OK, list);
        }
        
        /// <summary>
        /// Gets the status of a given workflow, containing all steps executed as well as assignments.
        /// </summary>
        /// <param name="id">The ID of the workflow record to retrieve status for.</param>
        /// <returns></returns>
        
        [Route("diagram/{id:int}")]
        public WorkflowDiagramModel GetWorkflowDiagram(int id, int? version = null)
        {
            var nodes = Company.Query<WorkflowDiagramNode>(QueryConstants.WorkflowDiagramNodes, new { id, version }).ToList();
            var links = Company.Query<WorkflowDiagramLink>(QueryConstants.WorkflowDiagramLinks, new { id, version }).ToList();
            var name = Company.Query<string>(@"select name from workflow.[type] where id = @id", new { id }).ToList().First().ToString();
            var type = Company.WorkflowTypes.Find(id);
            var @event = Company.WorkflowEventRegistrations.Single(e => e.TypeID == id);

            var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First();
            var publishedVersion = Company.WorkflowVersions.Find(type.PublishedVersionID);

            nodes.ForEach(n =>
            {
                n.SettingsObject = XmlToDynamic(n.Settings, false);
                n.FieldsObject = XmlToDynamic(n.Fields);
            });

            links.ForEach(l =>
            {
                l.ConditionObject = XmlToDynamic(l.Condition);
                l.SettingsObject = XmlToDynamic(l.Settings);
            });

            @event.ConditionObject = XmlToDynamic(@event.Condition);
            @event.SettingsObject = XmlToDynamic(@event.Settings, false);

            return new WorkflowDiagramModel
            {
                Nodes = nodes,
                Links = links,
                Type = type,
                Event = @event,
                CurrentVersion = currentVersion?.Version,
                PublishedVersion = publishedVersion?.Version ?? -1
            };
        }

        [HttpPost, Route("SubmitWorkflowForm/{itemId:int}/{itemStepId:int}")]
        public HttpResponseMessage SubmitWorkflowForm(int itemId, int itemStepId, List<WorkflowFormModelField> model)
        {
            try
            {
                int numberOfResponses = 1;
                int totalResources = 0;
                var item = Company.WorkflowItems.Where(x => x.ID == itemId).FirstOrDefault();
                var itemStepsModel = Company.WorkflowItemSteps.Where(x => x.ID == itemStepId).FirstOrDefault();

                var versionStep = Company.WorkflowVersionSteps.Where(x => x.ID == itemStepsModel.StepID).FirstOrDefault();

                if(itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "CANNOT FIND THE ITEM STEP FOR THE SPEICIFIED PARAMETERS");
                }

                if (string.IsNullOrEmpty(itemStepsModel.Settings))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Form settings is missing or invalid");

                var formSettings = WorkflowItemStepSettingModel.ParseXml(XElement.Parse(versionStep.Settings));
                var isCompleted = false;
                
                StringBuilder sb = new StringBuilder();

                var root = XElement.Parse(itemStepsModel.Fields);

                //increment the number of responses attribute

                if(root.Attribute("NumberOfResponses") != null)
                {                    
                    int.TryParse((string)root.Attribute("NumberOfResponses"), out numberOfResponses);                    
                    root.Attribute("NumberOfResponses").SetValue(++numberOfResponses);
                }
                else
                {
                    root.Add(new XAttribute("NumberOfResponses", numberOfResponses));
                }

                if(root.Attribute("TotalResources") != null)
                {
                    int.TryParse((string)root.Attribute("TotalResources"), out totalResources);                    
                }

                var newForm = new XElement("form", new XAttribute("ResourceID", Company.CurrentResourceID));

                foreach (var field in model)
                {
                    var val = field.Value != null ? field.Value.ToString() : "";

                    if (field.FieldType == WorkflowFormModelFieldType.boolean)
                    {
                        val = (val ?? "").ToUpper() == "TRUE" ? "TRUE" : "FALSE";
                    }

                    newForm.Add(new XElement("field",
                            new XAttribute("id", field.ID),
                            new XAttribute("label", field.Label),
                            new XAttribute("value", val),
                            new XAttribute("fieldtype", field.FieldType.ToString().ToLower()))
                        );
                }

                root.Add(newForm);

                if (itemStepsModel == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Unable to find item step");
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
                        //isCompleted = numberOfResponses >= (totalResources / 2) + 1;
                        isCompleted = numberOfResponses >= totalResources;
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

                var assignment = Company.WorkflowItemAssignments.Where(x => x.ItemID == itemId && x.ResourceObject == "Resource" && x.ResourceObjectID == Company.CurrentResourceID).FirstOrDefault();

                if(assignment!= null)
                {
                    Company.WorkflowItemAssignments.Remove(assignment);
                }

                Company.SaveChanges();

                var @object = (SystemObjects)Enum.Parse(typeof(SystemObjects), item.Object);
                
                var obj = Company.GetObjectDetail(@object, item.ObjectID);

                var type  = (SystemObjects)Enum.Parse(typeof(SystemObjects), obj.Type);

                if (isCompleted)
                {
                    Company.MarkStepAsCompleteAndContinue(itemStepsModel, itemId, new core.queue.EventObjectInfo { Object = @object, ObjectID = item.ObjectID, ObjectTypeID = obj.TypeID, ObjectType = type });
                }

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
            var itemStep = Company.WorkflowItemSteps.Where(x => x.ID == itemStepID).Include(x=>x.Item).Include(x=>x.Step).FirstOrDefault();

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


            ObjectDetail details = null;
            ObjectDetail issueItemDetails = null;
            var issueTypeName = "";
            var issueObjectType = "";

            switch (itemStep.Item.Object)
            {
                case "Issue":
                    var issue = Company.Issues.Where(x => x.ID == itemStep.Item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                    var comment = Company.Comments.Where(x => x.ID == issue.CommentID).FirstOrDefault();
                    if (issue != null)
                    {
                        details = new ObjectDetail
                        {
                            Type = "Action",
                            Name = comment != null ? comment.Body : "",
                            TypeName = issue.IssueType.Name
                        };

                        if(issue.IssueType != null)
                            issueTypeName = issue.IssueType.Name;
                        issueItemDetails = Company.GetObjectDetail(issue.Object, issue.ObjectID);
                        issueObjectType = issue.Object;
                    }
                    break;
                default:
                    details = Company.GetObjectDetail(itemStep.Item.Object, itemStep.Item.ObjectID);
                    break;
            }

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
            var IsUserAllowedToComplete = Company.WorkflowItemAssignments.Where(x => x.ItemID == itemStep.ItemID && x.ResourceObjectID == Company.CurrentResourceID).Any();


            //parse the xml to get the form info

            return Request.CreateResponse<dynamic>(HttpStatusCode.OK, new
            {
                Fields = properties,
                Title = title ?? "",
                Description = desc ?? "",
                IsCompleted = itemStep.CompletedOn.HasValue || isCompletedByCurrentUser,
                ObjectName = details == null ? "(unknown)" : details.Name,
                ObjectType = itemStep.Item.Object,
                ObjectID = itemStep.Item.ObjectID,
                IsUserAllowedToComplete = IsUserAllowedToComplete,
                IssueObject = issueObjectType,
                IssueObjectID = issueItemDetails != null ? issueItemDetails.ID : 0,
                IssueObjectName = issueItemDetails != null ? issueItemDetails.Name : "",
                IssueTypeName = issueTypeName
            });
        }

        [Route("activitytypes"), HttpGet]
        public List<core.enums.Workflow.ActivityTypeInfo> GetActivityTypes()
        {
            return d360.core.enums.Workflow.WorkflowActivityType.EmailNotification.GetList().ToList();
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
	                        ,od.name as 'Name'
                            ,od.NgUrl as 'Url'
                            ,i.id as 'ItemID'
                          from
	                        [workflow].[version] v
	                        inner join [workflow].item i on v.id = i.versionid
	                        inner join [cache].objectdetails od on i.objectid = od.objectid and i.[object] = od.[object]                            
                          where 
	                        v.id = @id
            ";

            var types = Company.Query<dynamic>(sql, new { id = versionId }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("item/detail/{itemId:int}"), HttpGet]
        public HttpResponseMessage GetItemDetail(int itemId)
        {
            var item = Company.WorkflowItems.Include(x => x.Version).Where(x => x.ID == itemId).FirstOrDefault();
            
            if (item == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Cannot find the specified workflow instance.");

            // get the itemsteps for this workflow instance

            var itemSteps = Company.WorkflowItemSteps.Where(x => x.ItemID == itemId);

            var stepIDs = itemSteps.Select(y => y.StepID).ToArray();
            var steps = Company.WorkflowVersionSteps.Where(x => stepIDs.Contains(x.ID)).ToList();
            var workflow = Company.WorkflowTypes.Where(x => x.ID == item.Version.TypeID).FirstOrDefault();

            ObjectDetail objectDetails = null;

            switch (item.Object)
            {
                case "Issue":
                    var issue = Company.Issues.Where(x => x.ID == item.ObjectID).Include(x => x.IssueType).FirstOrDefault();

                    var comment = Company.Comments.Where(x => x.ID == issue.CommentID).FirstOrDefault();
                    if (issue != null)
                    {
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
                    ObjectDetails = objectDetails                    
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
            if (changeType == ChangeType.Loaded)
                types = types.Where(t => t.type == "Fusion").OrderBy(t => t.name).ToList();
            else
                types = types.Where(t => t.type != "Fusion").OrderBy(t => t.name).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, types);
        }

        [Route("type/{id:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowType(int id)
        {
            var type = Company.WorkflowTypes.Find(id);
            if (type == null || type.State != core.enums.State.Active)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type id {id} could not be found");

            var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First();
            var model = GetWorkflowDiagram(id, currentVersion?.Version);

            model.Type = type;
            model.CurrentVersion = currentVersion?.Version ?? 1;

            return Request.CreateResponse(HttpStatusCode.OK, model);
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
                        type.State = core.enums.State.Active;

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
                        type.Name = model.Type.Name;
                        type.Description = model.Type.Description;
                        type.UpdatedOn = DateTime.UtcNow;
                        type.UpdatedBy = Company.CurrentResourceID;
                        
                       
                        var currentVersion = Company.WorkflowVersions.Where(v => v.TypeID == type.ID).OrderByDescending(v => v.Version).First();
                        versionID = currentVersion.ID;

                        //the current version is published
                        if (type.PublishedVersionID == versionID && model.Nodes.Count > 0 && model.Links.Count > 0)
                        {

                            var version = new WorkflowVersion();
                            version.TypeID = type.ID;
                            version.CreatedBy = Company.CurrentResourceID;
                            version.CreatedOn = DateTime.UtcNow;
                            version.UpdatedBy = Company.CurrentResourceID;
                            version.UpdatedOn = DateTime.UtcNow;
                            version.Version = currentVersion.Version + 1;

                            Company.WorkflowVersions.Add(version);
                            Company.SaveChanges();

                            //create a new version
                            if (model.Type.PublishedVersionID == null)
                            {
                                versionID = version.ID;
                                newVersion = true;
                            } 
                            else
                            {
                                versionID = version.ID;
                                type.PublishedVersionID = version.ID;
                                newVersion = true;
                            }
                        } 
                        //current version is not published
                        else if (type.PublishedVersionID != versionID && model.Nodes.Count > 0 && model.Links.Count > 0)
                        {
                            //publish it
                            if (model.Type.PublishedVersionID != null)
                            {
                                type.PublishedVersionID = versionID;
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

                    Dictionary<int, int> keyMapping = new Dictionary<int, int>();
                    
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
                                step.Settings = JsonConvert.DeserializeXNode(n.Settings).ToString();
                                step.State = core.enums.State.Active;

                                if (string.IsNullOrEmpty(n.Fields))
                                    step.Fields = null;
                                else
                                    step.Fields = JsonConvert.DeserializeXNode(n.Fields).ToString();

                                Company.Add(step);
                                Company.SaveChanges();
                                keyMapping.Add(id, step.ID);
                            });
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
                                if (!string.IsNullOrEmpty(l.Condition))
                                {
                                    dynamic condition = JsonConvert.DeserializeObject(l.Condition);

                                    if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count > 0)
                                    {
                                        for (int i = 0; i < condition.Conditions.Condition.Count; i++)
                                        {
                                            var c = condition.Conditions.Condition[i];

                                            if (c["@VersionStepID"] != null)
                                            {
                                                condition.Conditions.Condition[i]["@VersionStepID"] = keyMapping[(int)c["@VersionStepID"]];
                                            }
                                        }
                                        l.Condition = JsonConvert.SerializeObject(condition);
                                    }
                                    else if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count == 0)
                                    {
                                        condition.Conditions = null;
                                        l.Condition = JsonConvert.SerializeObject(condition);
                                    }
                                    else
                                    {
                                        l.Condition = JsonConvert.SerializeObject(condition);
                                    }
                                }

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
                            //TODO: parse nodes and add
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
                                    step.Settings = JsonConvert.DeserializeXNode(n.Settings).ToString();
                                    step.State = core.enums.State.Active;

                                    if (string.IsNullOrEmpty(n.Fields))
                                        step.Fields = null;
                                    else
                                        step.Fields = JsonConvert.DeserializeXNode(n.Fields).ToString();

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

                                var link = Company.WorkflowVersionStepTransitions.SingleOrDefault(v => v.FromVersionStepID == from && v.ToVersionStepID == to);



                                if (fromNew || toNew || link == null)
                                {
                                    if (link == null)
                                        link = new WorkflowVersionStepTransition();

                                    link.FromVersionStepID = keyMapping[from];
                                    link.ToVersionStepID = keyMapping[to];
                                    link.Name = l.Name ?? "";
                                    link.TransitionType = l.TransitionType;

                                    //need to map new form conditions to their appropriate step id's 
                                    if (!string.IsNullOrEmpty(l.Condition))
                                    {
                                        dynamic condition = JsonConvert.DeserializeObject(l.Condition);

                                        if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count > 0)
                                        {
                                            for (int i = 0; i < condition.Conditions.Condition.Count; i++)
                                            {
                                                var c = condition.Conditions.Condition[i];

                                                if (c["@VersionStepID"] != null)
                                                {
                                                    condition.Conditions.Condition[i]["@VersionStepID"] = keyMapping[(int)c["@VersionStepID"]];
                                                }
                                            }
                                            l.Condition = JsonConvert.SerializeObject(condition);
                                        }
                                        else if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count == 0)
                                        {
                                            condition.Conditions = null;
                                            l.Condition = JsonConvert.SerializeObject(condition);
                                        }
                                        else
                                        {
                                            l.Condition = JsonConvert.SerializeObject(condition);
                                        }
                                    }

                                    link.Condition = JsonConvert.DeserializeXNode(l.Condition).ToString();
                                    link.Settings = JsonConvert.DeserializeXNode(l.Settings).ToString();
                                    link.FromPortID = l.FromPortID;
                                    link.ToPortID = l.ToPortID;
                                    link.State = core.enums.State.Active;

                                    Company.Add(link);
                                }
                                else
                                {
                                    //var link = Company.WorkflowVersionStepTransitions.SingleOrDefault(v => v.FromVersionStepID == from && v.ToVersionStepID == to);

                                    var existing = existingLinks.Find(t => t.FromVersionStepID == link.FromVersionStepID && t.ToVersionStepID == link.ToVersionStepID);
                                    if (existing != null) existingLinks.Remove(existing);

                                    if (link != null)
                                    {
                                        link.Name = l.Name ?? "";
                                        link.TransitionType = l.TransitionType;
                                        link.FromPortID = l.FromPortID;
                                        link.ToPortID = l.ToPortID;

                                        //need to map new form conditions to their appropriate step id's 
                                        if (!string.IsNullOrEmpty(l.Condition))
                                        {
                                            dynamic condition = JsonConvert.DeserializeObject(l.Condition);

                                            if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count > 0)
                                            {
                                                for (int i = 0; i < condition.Conditions.Condition.Count; i++)
                                                {
                                                    var c = condition.Conditions.Condition[i];

                                                    if (c["@VersionStepID"] != null)
                                                    {
                                                        condition.Conditions.Condition[i]["@VersionStepID"] = keyMapping[(int)c["@VersionStepID"]];
                                                    }
                                                }
                                                l.Condition = JsonConvert.SerializeObject(condition);
                                            }
                                            else if (condition.Conditions.Condition != null && condition.Conditions.Condition.Count == 0)
                                            {
                                                condition.Conditions = null;
                                                l.Condition = JsonConvert.SerializeObject(condition);
                                            }
                                            else
                                            {
                                                l.Condition = JsonConvert.SerializeObject(condition);
                                            }
                                        }



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

        [Route("type/{id:int}/delete")]
        public HttpResponseMessage DeleteWorkflow(int id)
        {
            var type = Company.WorkflowTypes.Find(id);

            if (type == null)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type ID {id} could not be found");

            if (type.State == core.enums.State.Deleted)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Workflow type ID {id} is already deleted");

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
        public HttpResponseMessage GetAssignedWorkflowInstances(int typeId, int resourceId = 0)
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
										else od.ObjectTypeName
									end as 'TypeName'
	                                ,od.ObjectType as 'ObjectType'
	                                ,od.ObjectTypeID as 'ObjectTypeID'
	                                ,coalesce(od.Name,'(unknown)') as 'ObjectName'
	                                ,wis.id as 'ItemStepID'
	                                ,wvs.name as 'StepName'
	                                ,wvs.steptype as 'StepType'
	                                ,wvs.activitytype as 'ActivityType'
                                    ,iss.[object] as 'IssueObject'
									,iss.[objectid] as 'IssueObjectID'
                                    ,cod.name as 'IssueObjectName'
                                from
	                                [workflow].[type] wt
	                                inner join [workflow].[version] wv on (wt.id = wv.typeid)
	                                inner join [workflow].[item] wi on (wv.id = wi.versionid)
	                                inner join [reporting].global_resource gr on (wi.startedby = gr.resourceid)
	                                left join [cache].objectdetails od on(od.[object] = wi.[object] and od.[objectid] = wi.[objectid])
	                                inner join [workflow].[itemassignment] wia on(wia.itemid = wi.id and wia.resourceobject = 'Resource' and wia.resourceobjectid = @r)
	                                inner join [workflow].[itemstep] wis on(wis.itemid = wi.id and wis.completedon is null)
	                                inner join [workflow].[versionstep] wvs on(wvs.id = wis.stepid)
                                    left outer join [dbo].[issue] iss on(wi.[objectid] = iss.id and wi.[object] = 'Issue')
                                    left outer join cache.objectdetails cod on (iss.objectid = cod.objectid and cod.[object] = iss.[object]) 
                                    left outer join [dbo].[issuetype] it on(iss.issuetypeid = it.id)
                                where
                                    wt.id = @typeId and wi.completedon is null and wvs.steptype = 2 and wvs.activitytype = 3
                           ";

                var w = Company.WorkflowTypes.Where(x => x.ID == typeId).FirstOrDefault();

                var res = Company.Query<dynamic>(sql, new { r = (resourceId > 0 ? resourceId : Company.CurrentResourceID), typeId = typeId });

                return Request.CreateResponse(new { items = res, workflow = w });
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("procedures"), HttpGet]
        public IQueryable GetWorkflowProcedures()
        {
            return Company.WorkflowTaskProcedures;
        }

        [Route("typelist"), HttpGet]
        public HttpResponseMessage GetWorkflowsByTypeList(string types)
        {
            //should only ever be comma separated list of numbers, remove anything else
            types = Regex.Replace(types ?? "", "[^0123456789, ]", string.Empty);

            types = types.Trim().TrimEnd(',');

            if (string.IsNullOrWhiteSpace(types))
                types = "-1";

            var results = Company.Query<dynamic>(string.Format(QueryConstants.WorkflowTypeList, types)).ToList();

            #region parse XML

            var responsibilitySql = @"
                select 
	                string_agg(r.FirstName + ' ' + r.LastName, ', ') as Resources 
                from
                (
	                select distinct 
		                r.* 
	                from ResponsibilityDetail d
	                left join [ResourceGroup] g on g.GroupID = d.ResponsibleObjectID and d.ResponsibleObjectType = 'Group'
	                left join reporting.Global_resource r on (r.ResourceID = d.ResponsibleObjectID and d.ResponsibleObjectType = 'Resource') 
		                or (r.ResourceID = g.ResourceID and d.ResponsibleObjectType = 'Group')
	                where ResponsibilityTypeID  = @id
                ) r";

            foreach(dynamic r in results)
            {
                if (r.CurrentStepID != null && r.Settings != null && r.ActivityType != null)
                {
                    var s = XmlToDynamic(r.Settings);

                    switch((WorkflowActivityType)r.ActivityType)
                    {
                        case WorkflowActivityType.Form:
                            if (s.SendFormEmail != null && (bool)s.SendFormEmail == true)
                            {
                                if (s.MessageRecipientType == EmailTaskRecipientType.Responsibility)
                                {
                                    if (s.ResponsibilityTypeID != null)
                                    {
                                        var resources = Company.Query<string>(responsibilitySql, new { id = (int)s.ResponsibilityTypeID });
                                        r.ResponsibleUser = resources;
                                    }
                                }
                                else if (s.MessageRecipientType == EmailTaskRecipientType.SpecificUser)
                                {
                                    r.ResponsibleUser = s.MessageToUser; 
                                }
                                else if (s.MessageRecipientType == EmailTaskRecipientType.Initiator)
                                {
                                    r.ResponsibleUser = r.StartedBy;
                                }
                                    
                            }
                            break;
                        case WorkflowActivityType.EmailNotification:
                            if (s.MessageRecipientType == EmailTaskRecipientType.Responsibility)
                            {
                                if (s.ResponsibilityTypeID != null)
                                {
                                    var resources = Company.Query<string>(responsibilitySql, new { id = (int)s.ResponsibilityTypeID });
                                    r.ResponsibleUser = resources;
                                }
                            }
                            else if (s.MessageRecipientType == EmailTaskRecipientType.SpecificUser)
                            {
                                r.ResponsibleUser = s.MessageToUser;
                            }
                            else if (s.MessageRecipientType == EmailTaskRecipientType.Initiator)
                            {
                                r.ResponsibleUser = r.StartedBy;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            #endregion

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("versionstep/history/{id:int}"), HttpGet]
        public HttpResponseMessage GetWorkflowVersionStepHistory(int id)
        {
            var results = Company.Query<dynamic>(QueryConstants.WorkflowVersionStepHistory, new { id }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        [Route("versionstep/history/{id:int}/excel.xls"), HttpGet]
        public HttpResponseMessage GetWorkflowVersionStepHistoryExcel(int id)
        {
            var results = Company.Query<dynamic>(QueryConstants.WorkflowVersionStepHistory, new { id }).ToList();

            #region Header

            var document = new SLDocument();
            document.AddWorksheet("History");

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
            var len = stream.Length;
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

        #region Helper Methods

        private dynamic XmlToDynamic(string xml, bool omitRootElement = true)
        {
            return string.IsNullOrEmpty(xml) ? JsonConvert.DeserializeObject("{}") : JsonConvert.DeserializeObject(JsonConvert.SerializeXNode(XElement.Parse(xml), Formatting.None, omitRootElement));
        }

        #endregion
    }
}
