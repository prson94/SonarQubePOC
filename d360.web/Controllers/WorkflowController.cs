using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.web.Models;
using d360.workflow;
using d360.workflow.entities;
using d360.web.Models.Attributes;

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
        
        [Route("ArtifactTypeWorkflowStatusOverlay"), NonNullableParameters]
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

        [Route("ChallengeNotification"), NonNullableParameters]
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

        [Route("CertificationNotification"), NonNullableParameters]
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
        
        #endregion
    }
}
