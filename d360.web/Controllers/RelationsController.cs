using System;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.core;
using d360.core.enums;
using d360.web.Models;
using d360.core.entities;
using System.Collections.Generic;
using System.Xml.Linq;
using Resources;

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

        [HttpGet]
        public JsonResult Classifications()
        {
            return Json(Company.GetClassifications().Select(i => new { ID = i.Key, Name = i.Value }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonNetResult Predicates()
        {

            return new JsonNetResult
            {
                Data = Company.Table<Predicate>().OrderBy(i => i.Name),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet]
        public JsonNetResult PredicatePhrases(int id)
        {

            return new JsonNetResult
            {
                Data = Company.Filter<PredicatePhrase>(i => i.PredicateID == id).OrderBy(i=>i.Phrase),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        //public JsonResult Roles(int IntersectTypeID)
        //{
        //    var models = Company.Filter<IntersectTypeRoleRelation>(i => i.IntersectTypeID == IntersectTypeID, i => i.IntersectTypeRole)
        //        .OrderBy(i => i.IntersectTypeRole.Name)
        //        .Select(i => new { ID = i.IntersectTypeRoleID, Name = i.IntersectTypeRole.Name });
        //    return Json(models, JsonRequestBehavior.AllowGet);
        //}

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

        public class OptionsToRelateDbModel
        {
            public string Menu { get; set; }
            public string SubMenu { get; set; }
            public string Type { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
        }

        public class OptionsToRelateJsonModel
        {
            public string html { get; set; }
            public List<OptionsToRelateJsonModel> items { get; set; }
        }

        public JsonNetResult OptionsToRelate()
        {
            #region SQL
            var sql = @"select	Menu,
		SubMenu,
		Type,
        ID,
		Name
from	(
		select	1 as SortOrder,
				'ArtifactType' as [Type],
				ID,
				Name,
				'Glossary' as Menu,
				'Artifacts' as SubMenu
		from	ArtifactType 
		union
		SELECT	1 as SortOrder,
				'TaxonomyType' as [Type],
				T.ID,
				T.Name as Name, --C.Name + ' : ' + 
				'Glossary' as Menu,
				'Models' as SubMenu
		FROM	TaxonomyType T
				--inner join TaxonomyTypeClass C on C.ID = T.TaxonomyTypeClassID
		union
		SELECT	4 as SortOrder,
				'DomainType' as [Type],
				ID,
				Name as Name,
				'Reference' as Menu,
				NULL as SubMenu
		FROM	DomainType
		union
		SELECT	3 as SortOrder,
				'PolicyType' as [Type],
				ID,
				Name as Name,
				'Events' as Menu,
				'Policies' as SubMenu
		FROM	PolicyType
		union
		SELECT	3 as SortOrder,
				'RuleType' as [Type],
				ID,
				Name as Name,
				'Events' as Menu,
				'Rules' as SubMenu
		FROM	(
				select 1 as ID, 'Informational' as Name
				union select 2 as ID, 'Quality Check' as Name
				union select 3 as ID, 'Metric' as Name
				union select 4 as ID, 'Profile' as Name
				) O
		union
		SELECT	5 as SortOrder,
				'Resource' as [Type],
				1,
				'Resource' as Name,
				'People' as Menu,
				NULL as SubMenu
		union
		SELECT	5 as SortOrder,
				'Group' as [Type],
				1,
				'Group' as Name,
				'People' as Menu,
				NULL as SubMenu
		union
		SELECT	2 as SortOrder,
				'FusionAttributeType' as [Type],
				A.ID,
				REPLACE(A.TextPath, T.Name + '.', '') as Name,
				'Fusion' as Menu,
				T.Name as SubMenu
		FROM	FusionAttributeType A
				inner join FusionType T on T.ID = A.FusionTypeID
		) O
order by	SortOrder, Menu, SubMenu, Name";
            #endregion

            var list = Company.Query<OptionsToRelateDbModel>(sql).ToList();
            var jsonItems = new List<OptionsToRelateJsonModel>();
            var jsonMenus = list.Select(i => new { i.Menu }).Distinct().ToList();
            var jsonSubMenus = list.Select(i => new { i.Menu, i.SubMenu }).Distinct().ToList();
            jsonMenus.ForEach(m =>
            {
                var menu = new OptionsToRelateJsonModel { html = string.Format("<span>{0}</span>", m.Menu) };
                if (jsonSubMenus.Any(i => i.Menu == m.Menu))
                {
                    menu.items = new List<OptionsToRelateJsonModel>();
                    foreach (var s in jsonSubMenus.Where(i => i.Menu == m.Menu))
                    {
                        var submenu = new OptionsToRelateJsonModel { html = string.Format("<span>{0}</span>", s.SubMenu) };
                        var addToSubMenu = !string.IsNullOrEmpty(s.SubMenu);

                        if (addToSubMenu)
                            submenu.items = new List<OptionsToRelateJsonModel>();

                        foreach (var listItem in list.Where(i => i.Menu == m.Menu && i.SubMenu == s.SubMenu))
                        {
                            var listItemMenu = new OptionsToRelateJsonModel { html = string.Format("<span data-a='Intersect' data-t='{0}' data-i='{1}'>{2}</span>", listItem.Type, listItem.ID, listItem.Name) };
                            if (addToSubMenu)
                                submenu.items.Add(listItemMenu);
                            else
                                menu.items.Add(listItemMenu);
                        }

                        if (addToSubMenu)
                            menu.items.Add(submenu);
                    }
                }

                jsonItems.Add(menu);
            });

            list = null;

            return new JsonNetResult { Data = jsonItems, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonNetResult PossibleRelationshipsBySource(string source, int id, string targetType, int targetTypeID)
        {
            var sql = "";

            if (targetType == "FusionAttributeType")
            {
                sql = @"select	D.[Object], D.ObjectID, F.Name + '.' + D.TextPath as Name, D.Url
from	cache.ObjectDetails D
		inner join FusionAttribute FA on D.[ObjectType] = @targetType and D.ObjectTypeID = @targetTypeID and FA.ID = D.ObjectID
		inner join Fusion F on F.ID = FA.FusionID
where	not exists  (
					select	1 
					from	[cache].[Relationships] R 
					where	R.SourceObject = @source 
							and R.SourceObjectID = @id
							and R.TargetObject = D.[Object] 
							and R.TargetObjectID = D.ObjectID
					)
order by F.Name, D.TextPath";
            }
            else
            {
                sql = @"select	D.[Object], D.ObjectID, D.TextPath as Name, D.Url
from	cache.ObjectDetails D
where	D.[ObjectType] = @targetType and D.ObjectTypeID = @targetTypeID
		and not exists (
						select	1 
						from	[cache].[Relationships] R 
						where	R.SourceObject = @source 
								and R.SourceObjectID = @id
								and R.TargetObject = D.[Object] 
								and R.TargetObjectID = D.ObjectID
						)
order by D.TextPath";
            }

            var items = Company.Query<dynamic>(sql, new { targetType, targetTypeID, source, id }).ToList();

            return new JsonNetResult { Data = items, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonResult RelationshipTypes(string type, int typeID)
        {
            var types = Company.GetAllowedIntersectionTypes(type, typeID);
            return Json(types, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("sources/predicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select P.ID as [value], R.Name as [group], P.Phrase as [text]
from Predicate R inner join PredicatePhrase P on P.PredicateID = R.ID
order by R.Name, P.Phrase");

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #endregion

        #region Partials

        public ActionResult AddRelationship(SystemObjects source, int sourceID, SystemObjects target, int targetID)
        {
            ViewData.Add("Source", source.ToString());
            ViewData.Add("SourceID", sourceID);
            ViewData.Add("TargetType", target.ToString());
            ViewData.Add("TargetTypeID", targetID);
            return PartialView();
        }

        [HttpGet, Route("sources/{type}/{id:int}/add")]
        public ActionResult AddSource(SystemObjects type, int id)
        {
            var dtl = Company.GetObjectDetail(type, id);
            ViewBag.ObjectName = dtl.Name;
            ViewBag.Object = type.ToString();
            ViewBag.ObjectID = id;
            dtl = null;
            return PartialView();
        }

        public class AddSourcePostModel
        {
            /// <summary>
            /// The current object that we are creating sources for.
            /// </summary>
            public string Target { get; set; }

            /// <summary>
            /// The current object's ID that we are creating sources for.
            /// </summary>
            public int TargetID { get; set; }

            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public int PredicatePhraseID { get; set; }
            public int IntersectID { get; set; }
        }

        public class IntersectLookupModel
        {
            public int IntersectID { get; set; }
            public int SubjectNodeID { get; set; }
            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public int ObjectNodeID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
        }

        [HttpPost, Route("sources")]
        public JsonNetResult AddSourcePost(AddSourcePostModel model)
        {
            var message = "";
            var success = false;

            if (string.IsNullOrEmpty(model.Target) || model.TargetID <= 0)
            {
                message = $"The Target, or current object, you provided is invalid.";
            }
            else
            {
                if (string.IsNullOrEmpty(model.Subject) || model.SubjectID <= 0)
                {
                    message = $"The Subject you provided is invalid.";
                }
                else
                {
                    if (string.IsNullOrEmpty(model.Object) || model.ObjectID <= 0)
                    {
                        message = $"The Object you provided is invalid.";
                    }
                    else
                    {
                        if (model.Subject == model.Object && model.SubjectID == model.ObjectID)
                        {
                            message = $"A source may not map to itself directly.";
                        }
                        else
                        {
                            Company.AddRelationship(model.Target, model.TargetID, model.Subject, model.SubjectID, IntersectClassification.Normal, null, null);
                            Company.AddRelationship(model.Target, model.TargetID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);

                            Company.AddRelationship(model.Subject, model.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
                            var intersect = Company.Query<IntersectLookupModel>(@"select S.IntersectID,
S.ID as SubjectNodeID, S.[ObjectType] as Subject, S.ObjectID as SubjectID,
O.ID as ObjectNodeID, O.[ObjectType] as [Object], O.ObjectID 
from [IntersectNode] S 
inner join IntersectNode O on O.IntersectID = S.IntersectID 
and S.[ObjectType] = @s and S.ObjectID = @sid 
and O.[ObjectType] = @o and O.ObjectID = @oid",
        new { s = model.Subject, sid = model.SubjectID, o = model.Object, oid = model.ObjectID }
        ).SingleOrDefault();
                            if (intersect != null)
                            {
                                // If we got here, we are all good.

                                var intersectMap = new IntersectMap
                                {
                                    MapID = 0,
                                    ObjectIntersectNodeID = intersect.ObjectNodeID,// objectIntersectNode.ID,
                                    PredicatePhraseID = model.PredicatePhraseID,
                                    SubjectIntersectNodeID = intersect.SubjectNodeID,// subjectIntersectNode.ID,
                                    Type = MapType.SourceToTarget
                                };
                                Company.Add<IntersectMap>(intersectMap);
                                success = true;
                            }
                            else
                            {
                                message = $"The Subject or Object did not match up with the Relationship you provided.";
                            }
                        }
                    }
                }
            }

            return new JsonNetResult {
                Data = new {
                    message = message,
                    success = success
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpDelete, Route("{target}/{targetID:int}/sources/{id:int}")]
        public JsonNetResult DeleteSource(SystemObjects target, int targetID, int id)
        {
            var message = "";
            var success = false;

            if (id <= 0)
            {
                message = $"The source ID ({id}) is invalid.";
            }
            else
            {
                var model = Company.GetById<IntersectMap>(id);
                if (model == null)
                {
                    message = $"The source with ID ({id}) could not be found.";
                }
                else
                {
                    if (!Company.HasPermission(target, targetID, Claim.Delete, ClaimObject.Relationship))
                    {
                        message = FormInfo.Permisions_Error_Delete;
                    }
                    else
                    {
                        Company.Delete<IntersectMap>(model);
                        success = true;
                    }
                }
            }

            return new JsonNetResult
            {
                Data = new
                {
                    message = message,
                    success = success
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
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

        public ActionResult AggregateRelationOverlay(SystemObjects type, int id, SystemObjects targetType, int targetID, int intersectTypeID, bool criticalOnly = false)
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
            ViewData.Add("intersectTypeID", intersectTypeID);
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
