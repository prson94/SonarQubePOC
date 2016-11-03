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
using d360.web.Filters;
using d360.core.exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        #region Models

        public class SourcesToObjectModel
        {
            public int ID { get; set; }
            public int IntersectID { get; set; }
            public int IntersectTypeID { get; set; }
            public string Type { get; set; }
            public bool IsStart { get; set; }
            public bool IsEnd { get; set; }
            public int Level { get; set; }
            public int NodeID { get; set; }
            public string TypeName { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string ObjectName { get; set; }
            public string O { get; set; }
            public int OID { get; set; }
            public string BackColor { get; set; }
            public string ForeColor { get; set; }
            public int PredicateID { get; set; }
            public string Predicate { get; set; }
            public int RawSourceRuleCount { get; set; }
            public int SourceRuleCount { get; set; } = 0;
            public int RawMappingRuleCount { get; set; }
            public int LinkMappingRuleCount { get; set; }
            public int ChallengeCount { get; set; }
            public int OpenEventCount { get; set; }
            public int OpenIssueCount { get; set; }
            public int RawTransformationCount { get; set; }
            public int LinkTransformationCount { get; set; }
        }

        /// <summary>
        /// This is the new model that corresponds to GetHierarchyByPredicateType stored procedure.
        /// </summary>
        public class HierarchyViewModel
        {
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string ObjectTypeName { get; set; }
            public int Level { get; set; }
            public int GroupNumber { get; set; }
        }

        public class HierarchyModel
        {
            public int ID { get; set; }
            public string Subject { get; set; }
            public string Object { get; set; }
            public int SubjectID { get; set; }
            public int ObjectID { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public string ParentID { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public string Url { get; set; }
            public string ObjectTypeName { get; set; }
            public int Level { get; set; }
            public int PredicateID { get; set; }
            public string PredicatePhrase { get; set; }
            public PredicateType Type { get; set; }
            public int GroupNumber { get; set; }
            public string UID { get; set; }

        }

        public class HierarchyArtifactsModel
        {
            public PredicateType MapType { get; set; }
            public SystemObjects Type { get; set; }
            public int ID { get; set; }
            public bool IsSubject { get; set; }
        }

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

        [HttpGet, Route("Classifications")]
        public JsonResult Classifications()
        {
            return Json(Company.GetClassifications().Select(i => new { ID = i.Key, Name = i.Value }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("IntersectRoles")]
        public JsonNetResult IntersectRoles()
        {
            var roles = Company.Table<IntersectRole>().OrderBy(i => i.Name);
            var usage = Company.Filter<Map>(i => i.IntersectRoleID.HasValue).Select(i => i.IntersectRoleID.Value).Distinct().ToList();
            var data = new List<dynamic>();

            roles.ToList().ForEach(p =>
            {
                data.Add(new
                {
                    ID = p.ID,
                    Name = p.Name,
                    Description = p.Description,
                    IsUsed = usage.Any(i => i == p.ID)
                });
            });

            return new JsonNetResult
            {
                Data = data,
                Formatting = Formatting.None
            };
        }

        [HttpGet, Route("Predicates")]
        public JsonNetResult Predicates()
        {
            var predicates = Company.Table<Predicate>().OrderBy(i => i.Name);
            var usage = Company.Filter<IntersectType>(i => i.PredicateID.HasValue).Select(i => i.PredicateID.Value).Distinct().ToList();
            var data = new List<dynamic>();

            predicates.ToList().ForEach(p =>
                {
                    data.Add(new
                    {
                        ID = p.ID,
                        Name = p.Name,
                        Inverse = p.Inverse,
                        p.IsSystem,
                        IsUsed = usage.Any(i => i == p.ID),
                        Type = p.Type.GetDisplayName()
                    });
            });

            return new JsonNetResult
            {
                Data = data,
                Formatting = Formatting.None
            };
        }

        [HttpGet, Route("GetPredicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select ID as [value], Name as [text] from Predicate order by Name");
            return new JsonNetResult { Data = list, Formatting = Formatting.None };
        }

        [Route("_IntersectTypes")]
        public JsonNetResult _IntersectTypes()
        {
            var models = Company.Query<dynamic>(
@"select    I.ID,
			I.Subject,
			I.SubjectID,
			SD.TextPath as SubjectName,
            I.PredicateID,
            P.Name as PredicateName,
			I.Object,
			I.ObjectID,
			TD.TextPath as ObjectName
from		IntersectType I
            left join [Predicate] P on P.ID = I.PredicateID
			left join cache.ObjectDetails SD on SD.[Object] = I.Subject and SD.ObjectID = I.SubjectID
			left join cache.ObjectDetails TD on TD.[Object] = I.Object and TD.ObjectID = I.ObjectID
where       I.IsSystem = 0
order by	SD.Name,
			TD.Name");
            return new JsonNetResult { Data = models, Formatting = Formatting.None };
        }

        [Route("OptionsToRelate")]
        public JsonNetResult OptionsToRelate(SystemObjects type, int id)
        {
            #region SQL
            var sql = @"
select  distinct
        RT.ID as IntersectTypeID,
        O.SortOrder,
        O.Menu,
		O.SubMenu,
		O.Type,
        O.ID,
		O.Name
from	(
		select	1 as SortOrder,
				'ArtifactType' as [Type],
				ID,
				Name,
				'Glossary' as Menu,
				NULL as SubMenu
		from	ArtifactType 
		union
		SELECT	1 as SortOrder,
				'TaxonomyType' as [Type],
				T.ID,
				T.Name as Name,
				'Models' as Menu,
				NULL as SubMenu
		FROM	TaxonomyType T
		union
		SELECT	4 as SortOrder,
				'DomainType' as [Type],
				ID,
				Name as Name,
				'Reference' as Menu,
				NULL as SubMenu
		FROM	DomainType
		union
		SELECT	5 as SortOrder,
				'FusionAttributeType' as [Type],
				T.ID,
				REPLACE(T.TextPath, FT.Name+'.','') as Name,
				'Fusion' as Menu,
				FT.Name as SubMenu
		FROM	FusionAttributeType T
                inner join FusionType FT on FT.ID = T.FusionTypeID
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
				'ResourceType' as [Type],
				1,
				'Resource' as Name,
				'People' as Menu,
				NULL as SubMenu
		union
		SELECT	5 as SortOrder,
				'GroupType' as [Type],
				1,
				'Group' as Name,
				'People' as Menu,
				NULL as SubMenu
		) O
		inner join [IntersectType] RT on (RT.Subject = O.[Type] and RT.SubjectID = O.[ID] and RT.Object = @type and RT.ObjectID = @id) 
										or (RT.Object = O.[Type] and RT.ObjectID = O.[ID] and RT.Subject = @type and RT.SubjectID = @id)  
order by	O.SortOrder, O.Menu, O.SubMenu, O.Name";
            #endregion

            var list = Company.Query<OptionsToRelateDbModel>(sql, new { type = type.ToString(), id }).ToList();
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
                            var listItemMenu = new OptionsToRelateJsonModel { html = $"<span data-a='Intersect' data-t='{listItem.Type}' data-i='{listItem.ID}' data-it='{listItem.IntersectTypeID}'>{listItem.Name}</span>" };
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

        [Route("RelationshipTypes")]
        public JsonResult RelationshipTypes(string type, int typeID)
        {
            var types = Company.GetAllowedIntersectionTypes(type, typeID);
            return Json(types, JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("PossibleRelationshipsByIntersect")]
        public JsonNetResult PossibleRelationshipsByIntersect(int id)
        {
            var list = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList().Select(i => new ContextToolbarItem {
                Context = ContextList.ActionRelate,
                Icon = "plus",
                Title = i.TargetName,
                Type = "local",
                Uri = "/form/AddRelationship?intersectTypeID=" + i.IntersectTypeID + "&type=Intersect&id=" + i.ParentIntersectID                
            });
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, Route("GetPossibleRelationshipsObjectByIntersect")]
        public JsonNetResult GetPossibleRelationshipsObjectByIntersect(int id)
        {
            var list = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList().Select(i => new 
            {                
                Title = i.TargetName,                
                IntersectTypeID = i.IntersectTypeID,
                ParentIntersectID = i.ParentIntersectID,
                ObjectType = i.TargetType
            });
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        #region Hierarchy

        [HttpGet, Route("hierarchy/{mapType}/{type}/{id:int}")]
        public JsonNetResult GetHierarchy(SystemObjects type, int id, PredicateType mapType)
        {
            return new JsonNetResult
            {
                Data = new { },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, Route("hierarchy/artifacts")]
        public JsonNetResult GetHierarchyArtifactsNg(HierarchyArtifactsModel model)
        {
            return GetHierarchyArtifacts(model);
        }

        [HttpGet, Route("hierarchy/artifacts")]
        public JsonNetResult GetHierarchyArtifacts(HierarchyArtifactsModel model)
        {
            return new JsonNetResult
            {
                Data = null,//itemList,
                Formatting = Formatting.None
            };
        }

        #endregion Hierarchy

        [HttpGet, Route("ChildRelationshipsBySourceAndTarget")]
        public JsonNetResult ChildRelationshipsBySourceAndTarget(SystemObjects s, int sID, SystemObjects t, int tID)
        {
            var sType = s.ToString();
            var tType = t.ToString();
            var sql = $@"
select T.Object,
		T.ObjectID,
		T.ObjectUrl,
		T.ObjectName,
		T.ObjectTypeName
from[Intersect] O
    inner join[IntersectDetail] T on (
                                       ( (O.Subject = @s and O.SubjectID = @sid) AND (O.Object = @o and O.ObjectID = @oid) ) OR
                                       ( (O.Subject = @o and O.SubjectID = @oid) AND (O.Object = @s and O.ObjectID = @sid) )
							        )
									and T.Subject = 'Intersect' and T.SubjectID = O.ID";

            return new JsonNetResult { Data = Company.Query<dynamic>(sql, new { s = new Dapper.DbString { Value = sType, IsAnsi = true }, sid = sID, o = new Dapper.DbString { Value = tType.ToString(), IsAnsi = true }, oid = tID }).OrderBy(i => i.ObjectTypeName).ThenBy(i => i.ObjectName), Formatting = Formatting.None };
        }

        JArray convertList(JToken i)
        {
            if (i == null)
            {
                return null;
            }
            else
            {
                if (i is JArray)
                {
                    return (JArray)i;
                }
                else
                {
                    return new JArray(i);
                }
            }
        }

        [HttpGet, Route("{type}/{id:int}/RelationshipTypeTree.json")]
        public JsonNetResult RelationshipTypeTree(SystemObjects type, int id)
        {
            var sql = $@"
select	IntersectTypeID,
		TargetObjectType,
		TargetObjectID,
		OD.TextPath,
        1 as [Level],
		(
		select	    IntersectTypeID,
				    TargetObjectType,
				    TargetObjectID,
				    ID.TextPath,
                    2 as [Level]
		from	    IntersectType I
				    inner join cache.ObjectDetails ID on ( 
                        (ID.[Object] = I.Object and ID.ObjectID = I.ObjectID and I.Subject = 'IntersectType' and I.SubjectID = O.IntersectTypeID) OR
                        (ID.[Object] = I.Subject and ID.ObjectID = I.SubjectID and I.Object = 'IntersectType' and I.ObjectID = O.IntersectTypeID)
                    )
        order by    ID.TextPath
		for         xml path('relationships'), TYPE
		),
		(
		select	    P.*
		from	    IntersectTypePredicate IP
				    inner join Predicate P on P.[Type] = IP.PredicateType and IP.IntersectTypeID = O.IntersectTypeID
		order by    P.Name
        for         xml path('predicates'), TYPE
		)
from	    IntersectType O
		    inner join cache.ObjectDetails OD on (
                (OD.[Object] = O.Object and OD.ObjectID = O.ObjectID and O.Subject = @type and O.SubjectID = @id) OR
                (OD.[Object] = O.Subject and OD.ObjectID = O.SubjectID and O.Object = @type and O.ObjectID = @id)
            )
order by    OD.TextPath
for		    xml path('relationship'), root('item')
";
            var xmls = Company.Query<string>(sql, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();
            var xml = string.Join<string>("", xmls);
            //var doc = XElement.Parse(xml);
            //var obj = JObject.Parse(JsonConvert.SerializeXNode(XElement.Parse(xml)));
            if (string.IsNullOrEmpty(xml))
            {
                return new JsonNetResult { Data = JArray.Parse("[]"), Formatting = Formatting.None };
            }
            else
            {
                try
                {
                    var rels = JObject.Parse(JsonConvert.SerializeXNode(XElement.Parse(xml)))["item"]["relationship"].Children().Select(i => new {
                        IntersectTypeID = i["IntersectTypeID"].Value<int>(),
                        TargetObjectType = i["TargetObjectType"],
                        TargetObjectID = i["TargetObjectID"].Value<int>(),
                        TextPath = i["TextPath"],
                        Level = i["Level"].Value<int>(),
                        relationships = convertList(i["relationships"]),
                        predicates = convertList(i["predicates"])
                    });
                    return new JsonNetResult
                    {
                        Data = rels,//.SelectTokens("item.relationship"), //The SelectTokens method adds an extra [] hierarchy at the top. 
                        Formatting = Formatting.None
                    };
                }
                catch
                {
                    return new JsonNetResult { Data = JArray.Parse("[]"), Formatting = Formatting.None };
                }
            }
        }

        #endregion

        #region Partials

        [Route("AggregateRelationOverlay")]
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
            ViewData.Add("source", source.Name);
            ViewData.Add("type", type);
            ViewData.Add("id", id);
            ViewData.Add("targetType", targetType);
            ViewData.Add("targetID", targetID);
            ViewData.Add("intersectTypeID", intersectTypeID);
            return PartialView();
        }

        [Route("Impact")]
        public ActionResult Impact(SystemObjects type, int id)
        {
            try
            {
                ViewBag.Object = type.ToString();
                ViewBag.ObjectID = id;
            }
            catch
            {
            }
            return PartialView();
        }

        [Route("Lineage")]
        public ActionResult Lineage(SystemObjects type, int id)
        {
            try
            {
                ViewBag.Object = type.ToString();
                ViewBag.ObjectID = id;
            }
            catch
            {
            }
            //var model = Company.GetObjectDetail(type, id);
            return PartialView();
        }

        [Route("RelationOverlay")]
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
            ViewData.Add("Object", type.ToString());
            ViewData.Add("ObjectID", id.ToString());
            var model = Company.GetObjectDetail(type, id);
            return PartialView(model);
        }

        [Route("ImpactAnalysisOverlay")]
        public ActionResult ImpactAnalysisOverlay(SystemObjects type, int id)
        {
            ViewData.Add("Object", type.ToString());
            ViewData.Add("ObjectID", id.ToString());
            var model = Company.GetObjectDetail(type, id);
            return PartialView(model);
        }

        #endregion
    }
}
