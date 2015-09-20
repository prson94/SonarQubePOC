using System;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.core;
using d360.core.enums;
using d360.web.Models;
using d360.core.entities;
using System.Collections.Generic;

namespace d360.web.Controllers
{
    [RoutePrefix("relations"), Authorize]
    public class RelationsController : BaseController
    {
        #region DI

        public RelationsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Json

        [Route("contexts")]
        public JsonResult IntersectContexts()
        {
            var model = (
                        from d in Company.Table<Domain>()
                        from i in d.Items
                        where d.Items.Count > 0
                        orderby d.DomainType.Name
                        orderby i.Name
                        select new
                        {
                            i.Code,
                            i.Name,
                            i.ID,
                            List = d.Name,
                            Type = d.DomainType.Name
                        })
                         .ToList();

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Classifications()
        {
            return Json(Company.GetClassifications().Select(i => new { ID = i.Key, Name = i.Value }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult Roles(int IntersectTypeID)
        {
            var models = Company.Filter<IntersectTypeRoleRelation>(i => i.IntersectTypeID == IntersectTypeID, i => i.IntersectTypeRole)
                .OrderBy(i => i.IntersectTypeRole.Name)
                .Select(i => new { ID = i.IntersectTypeRoleID, Name = i.IntersectTypeRole.Name });
            return Json(models, JsonRequestBehavior.AllowGet);
        }

        public class IntersectTypeListViewModel
        {
            public int ID { get; set; }
            public string SourceType { get; set; }
            public int SourceID { get; set; }
            public string SourceTypeName { get; set; }
            public string SourceName { get; set; }
            public string TargetType { get; set; }
            public int TargetID { get; set; }
            public string TargetTypeName { get; set; }
            public string TargetName { get; set; }
        }

        public JsonResult _IntersectTypes()
        {
            var models = Company.Query<IntersectTypeListViewModel>(
@"select    I.ID,
			S.ObjectType as SourceType,
			S.ObjectID as SourceID,
			SD.ObjectTypeName as SourceTypeName,
			SD.TextPath as SourceName,
			T.ObjectType as TargetType,
			T.ObjectID as TargetID,
			TD.ObjectTypeName as TargetTypeName,
			TD.TextPath as TargetName
from		IntersectType I
			inner join IntersectTypeNode S on S.IntersectTypeID = I.ID and S.[Order] = 1
			inner join IntersectTypeNode T on T.IntersectTypeID = I.ID and T.ID <> S.ID
			left join cache.ObjectDetails SD on SD.[Object] = S.ObjectType and SD.ObjectID = S.ObjectID
			left join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID
order by	SD.ObjectTypeName,
			SD.TextPath,
			TD.ObjectTypeName,
			TD.TextPath");
            return Json(models, JsonRequestBehavior.AllowGet);
        }

        public JsonResult PossibleRelationshipsBySource(string source, int id, string targetType, int targetTypeID, int intersectTypeID)
        {
            source = source.Replace("Type", "");
            if (targetType != SystemObjects.EventType.ToString())
                targetType = targetType.Replace("Type", "");

            var items = Company.GetPossibleRelationshipsBySourceAndTargetType(
                (SystemObjects)Enum.Parse(typeof(SystemObjects), source),
                id,
                (SystemObjects)Enum.Parse(typeof(SystemObjects), targetType),
                targetTypeID,
                intersectTypeID);
            //, i.ConstrainingObjectID, i.ConstrainingObjectType
            return Json(items.Select(i => new { i.TargetID, i.TargetName, TargetType = targetType, IntersectTypeID = intersectTypeID }).OrderBy(i => i.TargetName), JsonRequestBehavior.AllowGet);
        }

        public JsonResult RelationshipTypes(string type, int typeID)
        {
            var types = Company.GetAllowedIntersectionTypes(type, typeID);
            return Json(types, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RelationshipTypesForAdding(string type, int typeID, int intersectID = 0)
        {
            var types = Company.GetAllowedIntersectionTypes(type, typeID, intersectID);
            return Json(
                types.Select(i => new { 
                    Name = i.TargetName, 
                    Value = string.Format("{0}|{1}|{2}", i.IntersectTypeID, i.TargetType, i.TargetTypeID), 
                    ParentIntersectID = i.ParentIntersectID
                }), 
                JsonRequestBehavior.AllowGet
                );
        }

        internal class SimpleHierarchyDbViewModel
        {
            public int ID { get; set; }
            public int? ParentID { get; set; }
            public int IntersectFlowID { get; set; }
            public string FlowTypeName { get; set; }

            public int FromIntersectNodeID { get; set; }
            public string FromObjectType { get; set; }
            public int FromObjectID { get; set; }
            public string FromObjectName { get; set; }
            public string FromObjectUrl { get; set; }

            public int ToIntersectNodeID { get; set; }
            public string ToObjectType { get; set; }
            public int ToObjectID { get; set; }
            public string ToObjectName { get; set; }
            public string ToObjectUrl { get; set; }
        }

        internal class SimpleHierarchyJsonViewModel
        {
            public SimpleHierarchyJsonViewModel()
            {
                Items = new List<SimpleHierarchyJsonViewModel>();   
            }

            public int IntersectFlowID { get; set; }
            public string FlowTypeName { get; set; }
            public string ObjectType { get; set; }
            public int ObjectID { get; set; }
            public string ObjectName { get; set; }
            public string ObjectUrl { get; set; }
            public List<SimpleHierarchyJsonViewModel> Items { get; set; }
        }

        public JsonNetResult SimpleHierarchies(SystemObjects type, int id)
        {
            var list = Company.Query<SimpleHierarchyDbViewModel>(@"
declare @Flows table (ID int, FlowTypeName nvarchar(250))

insert into @Flows
	select	F.ID,
            FT.Name
	from	IntersectFlow F
            inner join IntersectFlowType FT on FT.ID = F.IntersectFlowTypeID and FT.IntersectFlowConfiguration = 1
			inner join IntersectFlowItem I on I.IntersectFlowID = F.ID
			inner join IntersectNode N on (N.ID = I.FromIntersectNodeID or N.ID = I.ToIntersectNodeID) and N.ObjectType = @type and N.ObjectID = @id;

with flows as
	(
	select	I.*,
			1 as [Level]
	from	IntersectFlowItem I
			inner join @Flows F on F.ID = I.IntersectFlowID and I.ParentID is null
	union all
	select	I.*,
			P.[Level] + 1 as [Level]
	from	IntersectFlowItem I
			inner join flows P on I.ParentID = P.ID
	)
select	F.ID,
        F.ParentID,
        F.IntersectFlowID,
        FT.FlowTypeName,
        F.FromIntersectNodeID,
        FN.ObjectType as FromObjectType,
        FN.ObjectID as FromObjectID,
        FD.Name as FromObjectName,
        FD.Url as FromObjectUrl,
        F.ToIntersectNodeID,
        TN.ObjectType as ToObjectType,
        TN.ObjectID as ToObjectID,
        TD.Name as ToObjectName,
        TD.Url as ToObjectUrl
from	flows F
        inner join @Flows FT on FT.ID = F.IntersectFlowID
		inner join IntersectNode FN on FN.ID = F.FromIntersectNodeID
        inner join cache.ObjectDetails FD on FD.[Object] =  FN.ObjectType and FD.ObjectID = FN.ObjectID
		inner join IntersectNode TN on TN.ID = F.ToIntersectNodeID
		inner join cache.ObjectDetails TD on TD.[Object] =  TN.ObjectType and TD.ObjectID = TN.ObjectID", new { type = type.ToString(), id }).ToList();

            var models = new List<SimpleHierarchyJsonViewModel>();

            //The distinct IDs we are getting here represent different flows altogether, meaning different hierarchies.
            var distinctFlowIDs = list.Select(i => i.IntersectFlowID).Distinct().ToList();

            distinctFlowIDs.ForEach(flowID =>
            {
                var flowItems = list.Where(i => i.IntersectFlowID == flowID).ToList();
                parseSimpleHierarchyJsonViewModel(models, flowItems);
            });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        void parseSimpleHierarchyJsonViewModel(List<SimpleHierarchyJsonViewModel> models, List<SimpleHierarchyDbViewModel> flowItems, int? parentID = null, SimpleHierarchyJsonViewModel parent = null)
        {
            bool shouldAddToModelCollection = false;

            foreach (var flowItem in flowItems.Where(i => i.ParentID == parentID))
            {
                SimpleHierarchyJsonViewModel model = null;

                if (parent == null)
                {
                    // We should only be in this loop the first time we cycle through to generate the hierarchy.
                    parent = new SimpleHierarchyJsonViewModel
                    {
                        FlowTypeName = flowItem.FlowTypeName,
                        IntersectFlowID = flowItem.IntersectFlowID,
                        ObjectID = flowItem.FromObjectID,
                        ObjectName = flowItem.FromObjectName,
                        ObjectType = flowItem.FromObjectType,
                        ObjectUrl = flowItem.FromObjectUrl
                    };
                    shouldAddToModelCollection = true;
                }

                model = new SimpleHierarchyJsonViewModel
                {
                    FlowTypeName = flowItem.FlowTypeName,
                    IntersectFlowID = flowItem.IntersectFlowID,
                    ObjectID = flowItem.ToObjectID,
                    ObjectName = flowItem.ToObjectName,
                    ObjectType = flowItem.ToObjectType,
                    ObjectUrl = flowItem.ToObjectUrl
                };
                parseSimpleHierarchyJsonViewModel(models, flowItems, flowItem.ID, model);   // Recurse
                parent.Items.Add(model);                                                    // Add to parent Items collection.
            }

            if (shouldAddToModelCollection)
            {
                models.Add(parent);         // We only add this to the model collections once per hierarchy flow.
            }
        }

        #endregion

        #region Partials

        public ActionResult AddRelationship(SystemObjects source, int sourceID, int intersectTypeID, SystemObjects target, int targetID)
        {
            ViewData.Add("Source", source.ToString());
            ViewData.Add("SourceID", sourceID);
            ViewData.Add("IntersectTypeID", intersectTypeID);
            ViewData.Add("TargetType", target.ToString());
            ViewData.Add("TargetTypeID", targetID);
            return PartialView();
        }

        public ActionResult EditRelationship(int id)
        {
            ViewData.Add("IntersectID", id);
            var intersect = Company.GetById<Intersect>(id);
            var model = new EditRelationshipModel {
                Classification = intersect.Classification ?? 0,
                Description = intersect.Description,
                IntersectTypeID = intersect.IntersectTypeID,
                Role = intersect.IntersectTypeRoleID
            };
            intersect = null;
            return PartialView(model);
        }

        public ActionResult AggregateRelationOverlay(SystemObjects type, int id, SystemObjects targetType, int targetID, bool criticalOnly = false)
        {
            var source = Company.GetObjectDetail(type, id);
            var target = Company.GetObjectDetail(targetType, targetID);

            ViewData.Add("Title", 
                string.Format("{0}{1} Relationships For {2}", 
                    (criticalOnly ? "Critical " : ""), 
                    ((target != null) ? target.Name : targetType.ToString()), 
                    ((source != null) ? source.Name : type.ToString())
                )
            );
            ViewData.Add("criticalOnly", criticalOnly);
            ViewData.Add("type", type);
            ViewData.Add("id", id);
            ViewData.Add("targetType", targetType);
            ViewData.Add("targetID", targetID);
            return PartialView();
        }

        public ActionResult RelationOverlay(SystemObjects type, int id)
        {
            try 
	        {	        
                ViewData.Add("CanCreateRelationships", Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship));
	        }
	        catch
	        {
                ViewData.Add("CanCreateRelationships", false);
	        }

            var model = Company.GetObjectDetail(type, id);
            return PartialView(model);
        }

        #endregion
    }
}
