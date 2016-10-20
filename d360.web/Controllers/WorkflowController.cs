using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.web.Models;
using d360.workflow;
using d360.workflow.entities;

namespace d360.web.Controllers
{
    [RoutePrefix("workflow"), Authorize]
    public class WorkflowController : BaseController
    {
        #region DI

        public WorkflowController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Models

        public class WorkflowTypeInfoModel: WorkflowTypeInfo
        {
            public List<WorkflowTypeStepInfoModel> Steps { get; set; }
        }

        public class WorkflowTypeStepInfoModel : WorkflowTypeStepInfo
        {
            public int Count { get; set; }
            public WorkflowType WorkflowType { get; set; }
        }

        public class WorkflowStepBreakdown
        {
            public WorkflowType WorkflowType { get; set; }
            public int ArtifactTypeID { get; set; }
            public int Step { get; set; }
            public int Count { get; set; }
        }

        public class ChallengeNotificationModel
        {
            public string Reason { get; set; }
            public Guid WorkflowID { get; set; }
            public int  ResourceID{ get; set; }
            public string ResourceName { get; set; }
            public string ResourceUrl { get; set; }
            public int AssignedResourceID { get; set; }
            public DateTime DateStarted { get; set; }
            public int CommentID { get; set; }
        }

        #endregion

        #region Partials

        [Route("{id}/overlay/{full?}")]
        public ActionResult WorkflowActionOverlay(Guid id, bool full = false)
        {
            var workflow = Company.GetById<Workflow>(id);
            ViewBag.WorkflowID = id;
            ViewBag.IsFullOverlay = full;
            return PartialView(string.Format("WorkflowActionOverlay_{0}", (int)workflow.WorkflowType));
        }

        [Route("ArtifactTypeWorkflowStatusOverlay")]
        public ActionResult ArtifactTypeWorkflowStatusOverlay(int id)
        {
            var type = Company.GetObjectDetail("ArtifactType", id);
            if (type == null) return HttpNotFound();
 
            ViewBag.ID = id;
            ViewBag.Type = type.PluralizedName;
            return PartialView();
        }

        #endregion

        #region Json

        [Route("ChallengeNotification")]
        public JsonNetResult ChallengeNotification(int id)
        {
            var sql = @"select top 1
			W.ID as WorkflowID,
            W.Data.value('(fields/Reason)[1]', 'nvarchar(500)')  as Reason,            
            R.ResourceID as ResourceID,
			R.FirstName + ' ' + R.LastName as ResourceName,
			dbo.GenerateObjectUrl('Resource', 0, R.ResourceID) as ResourceUrl,
            RES.ResourceID as AssignedResourceID,
            W.DateStarted as DateStarted,
            W.Data.value('(fields/CommentID)[1]', 'int')  as CommentID         
from		Workflow W            			                
            inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/RequestingResourceID)[1]', 'int')            
            left outer join WorkflowResource RES on RES.WorkflowID = W.ID and RES.ResourceID = @res
where
            W.WorkflowType = 4
		    and W.Data.exist('/fields/ArtifactID[text() = sql:variable(""@id"")]') = 1
            and W.DateCompleted is null                
order by	W.DateStarted desc";
            var workflowInfo = Company.Query<ChallengeNotificationModel>(sql, new { id = id, res = Company.CurrentResourceID }).FirstOrDefault();
            if (workflowInfo != null)
            {                
                return new JsonNetResult { Formatting = Newtonsoft.Json.Formatting.None, Data = workflowInfo };
            }
            else {
                return new JsonNetResult { Formatting = Newtonsoft.Json.Formatting.None, Data = new { } };
            }
        }

        [Route("CertificationNotification")]
        public JsonNetResult CertificationNotification(int id)
        {
            var sql = @"select top 1
			W.ID
from		Workflow W
			inner join WorkflowResource WR on WR.WorkflowID = W.ID 
                and WR.ResourceID = @r
                and W.WorkflowType = 2
			    and W.Data.exist('/fields/ArtifactID[text() = sql:variable(""@id"")]') = 1
			    and W.DateCompleted is null
                and WR.IsComplete = 0
order by	W.DateStarted desc";
            Guid workflowID = Company.Query<Guid>(sql, new { r = Company.CurrentResourceID, id = id }).SingleOrDefault();
            if (workflowID != Guid.Empty)
            {
                return new JsonNetResult { Formatting = Newtonsoft.Json.Formatting.None, Data = new { WorkflowID = workflowID } };
            }
            else {
                return new JsonNetResult { Formatting = Newtonsoft.Json.Formatting.None, Data = new { } };
            }
        }

        [Route("WorkflowStepBreakdownByArtifactType")]
        public JsonNetResult WorkflowStepBreakdownByArtifactType(int id)
        {
            var sql = @"select		*,
			count(1) as [Count]
from		(
			select	W.WorkflowType,
					coalesce(A.ArtifactTypeID, W.Data.value('(/fields/ArtifactTypeID)[1]', 'int')) as ArtifactTypeID,
					W.Step
			from	Workflow W
					left join Artifact A on A.ID = W.Data.value('(/fields/ArtifactID)[1]', 'int')
            where   coalesce(A.ArtifactTypeID, W.Data.value('(/fields/ArtifactTypeID)[1]', 'int')) = @id
			) W
group by	WorkflowType,
			ArtifactTypeID,
			Step";
            var breakdowns = Company.Query<WorkflowStepBreakdown>(sql, new { id = id }).ToList();

            var models = WorkflowType.CertifyArtifact
                .GetWorkflowTypeEnumList()
                .Select(i => new WorkflowTypeInfoModel
                    {
                        ID = i.ID,
                        Name = i.Name,
                        Description = i.Description,
                        Steps = i.ID.GetWorkflowTypeStepsEnumList().Select(s => new WorkflowTypeStepInfoModel { ID = s.ID, Name = s.Name }).ToList()
                    }).Where(x=> (x.ID != WorkflowType.ChallengeArtifact && x.ID != WorkflowType.WorkIssue)).ToList();
            
            models.ForEach(t =>
            {
                t.Steps.ForEach(s =>
                {
                    var breakdown = breakdowns.SingleOrDefault(i => i.WorkflowType == t.ID && i.Step == s.ID);
                    s.Count = (breakdown != null) ? breakdown.Count : 0;
                    s.WorkflowType = t.ID;
                });
            });

            return new JsonNetResult 
            { 
                Data = models, 
                Formatting = Newtonsoft.Json.Formatting.None 
            };
        }

        [Route("WorkflowsByArtifactTypeAndWorkflowTypeAndStep")]
        public JsonNetResult WorkflowsByArtifactTypeAndWorkflowTypeAndStep(int id, WorkflowType type, int step, bool? isNg = false)
        {
            string sql = "";
            var columns = new List<GridColumn>();
            var fields = new List<GridField>();

            fields.Add(new GridField { name = "ID", type = "string" });
            fields.Add(new GridField { name = "ResourcesAssigned", type = "number" });
            fields.Add(new GridField { name = "ResourcesCompleted", type = "number" });

            switch (type)
            { 
                case WorkflowType.CertifyArtifact:
                    #region SQL
                    if (!isNg.GetValueOrDefault()) sql = @"
select	W.ID,
		W.Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
		W.Data.value('(/fields/StartDate)[1]', 'datetime') as StartDate,
		W.Data.value('(/fields/DueDate)[1]', 'datetime') as DueDate,
		W.DateCompleted,
		'<a data-context=""Preview"" data-type=""Artifact"" data-id=""' + cast(A.ID as varchar(15)) + '"" href=""' + dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) + '"">' + A.Name + '</a>' as Artifact,
		WA.[Count] as ResourcesAssigned,
		WC.[Count] as ResourcesCompleted
from	Workflow W
		inner join Artifact A on A.ID = W.Data.value('(/fields/ArtifactID)[1]', 'int')
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID
					) WA
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID and IsComplete = 1
					) WC
where   coalesce(A.ArtifactTypeID, W.Data.value('(/fields/ArtifactTypeID)[1]', 'int')) = @id
		and W.WorkflowType = 2
		and W.Step = @step";
                    else
                        sql = @"
select	W.ID,
		W.Data.value('(/fields/ArtifactID)[1]', 'int') as ArtifactID,
		W.Data.value('(/fields/StartDate)[1]', 'datetime') as StartDate,
		W.Data.value('(/fields/DueDate)[1]', 'datetime') as DueDate,
		W.DateCompleted,
		A.Name as Artifact,
		WA.[Count] as ResourcesAssigned,
		WC.[Count] as ResourcesCompleted
from	Workflow W
		inner join Artifact A on A.ID = W.Data.value('(/fields/ArtifactID)[1]', 'int')
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID
					) WA
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID and IsComplete = 1
					) WC
where   coalesce(A.ArtifactTypeID, W.Data.value('(/fields/ArtifactTypeID)[1]', 'int')) = @id
		and W.WorkflowType = 2
		and W.Step = @step";
                    #endregion
                    columns.Add(new GridColumn { columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "Artifact", filterable = true, filtertype = GridColumn.FILTER_TYPE_STRING, sortable = true, text = "Item", width = "35%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "StartDate", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Started On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DueDate", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Due On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DateCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Completed On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesAssigned", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Assigned", width = "10%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Completed", width = "10%" });
                    fields.Add(new GridField { name = "ArtifactID", type = "number" });
                    fields.Add(new GridField { name = "StartDate", type = "date" });
                    fields.Add(new GridField { name = "DueDate", type = "date" });
                    fields.Add(new GridField { name = "DateCompleted", type = "date" });
                    fields.Add(new GridField { name = "Artifact", type = "string" });

                    break;
                case WorkflowType.SuggestNewArtifact:
                    #region SQL
                    sql = @"select	W.ID,
		W.Data.value('(/fields/ArtifactTypeID)[1]', 'int') as ArtifactTypeID,
		W.Data.value('(/fields/Name)[1]', 'nvarchar(250)') as Name,
		W.Data.value('(/fields/Description)[1]', 'nvarchar(4000)') as Description,
		W.Data.value('(/fields/RequestingResourceID)[1]', 'int') as RequestingResourceID,
		R.FirstName + ' ' + R.LastName as RequestingResource,
		W.Data.value('(/fields/TaxonomyTypeID)[1]', 'int') as TaxonomyTypeID,
		V.Name as OwningModel,
		W.DateStarted,
        W.DateCompleted,
		WA.[Count] as ResourcesAssigned,
		WC.[Count] as ResourcesCompleted
from	Workflow W
		inner join TaxonomyType V on V.ID = W.Data.value('(/fields/TaxonomyTypeID)[1]', 'int')
		inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(/fields/RequestingResourceID)[1]', 'int')
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID
					) WA
		cross apply (
					select	count(1) as [Count]
					from	WorkflowResource
					where	WorkflowID = W.ID and IsComplete = 1
					) WC
where   W.Data.value('(/fields/ArtifactTypeID)[1]', 'int') = @id
		and W.WorkflowType = 1
		and W.Step = @step";
                    #endregion
                    columns.Add(new GridColumn { columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "Name", filterable = true, filtertype = GridColumn.FILTER_TYPE_STRING, sortable = true, text = "Name", width = "18%" });
                    columns.Add(new GridColumn { columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "RequestingResource", filterable = true, filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST, sortable = true, text = "Requestor", width = "17%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DateStarted", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Started On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DateCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Completed On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesAssigned", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Assigned", width = "10%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Completed", width = "10%" });
                    fields.Add(new GridField { name = "ArtifactTypeID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "Description", type = "string" });
                    fields.Add(new GridField { name = "RequestingResourceID", type = "number" });
                    fields.Add(new GridField { name = "RequestingResource", type = "string" });
                    fields.Add(new GridField { name = "DateStarted", type = "date" });
                    fields.Add(new GridField { name = "DateCompleted", type = "date" });
                    break;
                case WorkflowType.SuggestNewArtifactMulti:
                    #region SQL
                    sql = @"select	W.ID,
		                            W.Data.value('(/fields/ArtifactTypeID)[1]', 'int') as ArtifactTypeID,
		                            W.Data.value('(/fields/Name)[1]', 'nvarchar(250)') as Name,
		                            W.Data.value('(/fields/Description)[1]', 'nvarchar(4000)') as Description,
		                            W.Data.value('(/fields/RequestingResourceID)[1]', 'int') as RequestingResourceID,
		                            R.FirstName + ' ' + R.LastName as RequestingResource,
		                            W.Data.value('(/fields/TaxonomyTypeID)[1]', 'int') as TaxonomyTypeID,
		                            V.Name as OwningModel,
		                            W.DateStarted,
                                    W.DateCompleted,
		                            WA.[Count] as ResourcesAssigned,
		                            WC.[Count] as ResourcesCompleted
                            from	Workflow W
		                            inner join TaxonomyType V on V.ID = W.Data.value('(/fields/TaxonomyTypeID)[1]', 'int')
		                            inner join reporting.Global_Resource R on R.ResourceID = W.Data.value('(/fields/RequestingResourceID)[1]', 'int')
		                            cross apply (
					                            select	count(1) as [Count]
					                            from	WorkflowResource
					                            where	WorkflowID = W.ID
					                            ) WA
		                            cross apply (
					                            select	count(1) as [Count]
					                            from	WorkflowResource
					                            where	WorkflowID = W.ID and IsComplete = 1
					                            ) WC
                            where   W.Data.value('(/fields/ArtifactTypeID)[1]', 'int') = @id
		                            and W.WorkflowType = 5
		                            and W.Step = @step";
                    #endregion
                    columns.Add(new GridColumn { columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "Name", filterable = true, filtertype = GridColumn.FILTER_TYPE_STRING, sortable = true, text = "Name", width = "18%" });
                    columns.Add(new GridColumn { columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "RequestingResource", filterable = true, filtertype = GridColumn.FILTER_TYPE_CHECKEDLIST, sortable = true, text = "Requestor", width = "17%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DateStarted", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Started On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "d", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "DateCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_DATE, sortable = true, text = "Completed On", width = "15%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesAssigned", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Assigned", width = "10%" });
                    columns.Add(new GridColumn { cellsformat = "n", columntype = GridColumn.COLUMN_TYPE_STRING, datafield = "ResourcesCompleted", filterable = true, filtertype = GridColumn.FILTER_TYPE_NUMBER, sortable = true, text = "# Completed", width = "10%" });
                    fields.Add(new GridField { name = "ArtifactTypeID", type = "number" });
                    fields.Add(new GridField { name = "Name", type = "string" });
                    fields.Add(new GridField { name = "Description", type = "string" });
                    fields.Add(new GridField { name = "RequestingResourceID", type = "number" });
                    fields.Add(new GridField { name = "RequestingResource", type = "string" });
                    fields.Add(new GridField { name = "DateStarted", type = "date" });
                    fields.Add(new GridField { name = "DateCompleted", type = "date" });
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
            {
                var models = Company.Query<dynamic>(sql, new { id = id, step = step }).ToList(); //type = (int)type, 

                return new JsonNetResult
                {
                    Data = new
                    {
                        Fields = fields,
                        Columns = columns,
                        Data = models
                    },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else
            {
                return new JsonNetResult
                {
                    Data = new { },
                    Formatting = Newtonsoft.Json.Formatting.None
                };            
            }
        }

        [Route("WorkflowResponsibilityTypeOptions")]
        public JsonNetResult WorkflowResponsibilityTypeOptions(string type, int id)
        {
            return new JsonNetResult { Data = Company.GetWorkflowResponsibilityTypeOptions(type, id).Select(i => new { Text = i.Name, Value = i.ID.ToString() }), Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("WorkflowParentTypeOptions")]
        public JsonNetResult WorkflowParentTypeOptions(WorkflowType workflowType, string type, int id)
        {
            return new JsonNetResult { Data = Company.GetWorkflowParentTypeOptions((int)workflowType, type, id).Select(i => new { Text = i.Name, Value = string.Format("{0}|{1}", i.LookupObjectType, i.LookupObjectID) }), Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion
    }
}
