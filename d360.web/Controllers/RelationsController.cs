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

        [HttpGet]
        public JsonResult Classifications()
        {
            return Json(Company.GetClassifications().Select(i => new { ID = i.Key, Name = i.Value }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
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

        [HttpGet]
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

        [HttpGet, Route("sources/predicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select ID as [value], Name as [text] from Predicate order by Name");
            return new JsonNetResult { Data = list, Formatting = Formatting.None };
        }

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

        public JsonResult RelationshipTypes(string type, int typeID)
        {
            var types = Company.GetAllowedIntersectionTypes(type, typeID);
            return Json(types, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
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

        [HttpGet]
        public JsonNetResult GetFieldsByIntersectMap(int id)
        {
            var sql = @" select t.Name, t.FriendlyName, t.[Type], f.Value, f.FormattedValue from field f
                        join fieldtype t on  f.fieldtypeid = t.id
						join intersectmap m on m.id = @id
						join intersectnode n on n.id = m.subjectintersectnodeid and f.objectid = n.intersectid
                        where f.objecttype = 'Intersect'";

            var fields = Company.Query<dynamic>(sql, new { id });
            return new JsonNetResult
            {
                Data = fields,
                Formatting = Formatting.None
            };
        }

        #region Hierarchy

        //[ValidateHttpAntiForgeryToken, HttpPost, Route("hierarchy/save")]
        //public JsonNetResult SaveHierarchy(HierarchyPostModel model)
        //{
        //    var message = "";

        //    if (string.IsNullOrEmpty(model.Subject) || model.SubjectID <= 0)
        //    {
        //        message = $"The Subject you provided is invalid.";
        //    }
        //    else if (string.IsNullOrEmpty(model.Object) || model.ObjectID <= 0)
        //    {
        //        message = $"The Object you provided is invalid.";
        //    }
        //    else if (model.Subject == model.Object && model.SubjectID == model.ObjectID)
        //    {
        //        message = $"A parent may not map to itself directly.";
        //    }
        //    else if ((int)model.HierarchyType < 1)
        //    {
        //        message = $"The hierarchy type is invalid.";
        //    }

        //    var intersectType = Company.Filter<IntersectType>(i => i.Predicate.Type == ((model.HierarchyType == PredicateType.GroupHierarchy) ? PredicateType.GroupHierarchy : PredicateType.TypeHierarchy)).FirstOrDefault();

        //    if (intersectType == null)
        //    {
        //        message = "No relationship type exists to fulfill this request.";
        //    }

        //    if (message == "")
        //    {
        //        Company.AddIntersect(model.Subject, model.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
        //    }


        //    if (string.IsNullOrEmpty(message))
        //    {
        //        return new JsonNetResult
        //        {
        //            Data = new { type = "success", title = "Success", message = "Updated structure" },
        //            Formatting = Formatting.None
        //        };
        //    }
        //    else
        //    {
        //        return new JsonNetResult
        //        {
        //            Data = new { type = "error", title = "An error occured", message = message },
        //            Formatting = Formatting.None
        //        };
        //    }
        //}

        //[ValidateHttpAntiForgeryToken, HttpPost]
        //[Route("hierarchy/edit")]
        //public JsonResult EditHierarchy(HierarchyPostModel model)
        //{
        //    var intersect = Company.GetById<Intersect>(model.IntersectID);

        //    if (intersect == null)
        //        return null;

        //    Company.Update(intersect);

        //    return null;

        //}

        //[HttpDelete]
        //[Route("hierarchy/delete/{id:int}")]
        //public JsonResult DeleteHierarchy(int id)
        //{
        //    try
        //    {
        //        var model = Company.GetById<Intersect>(id);
        //        var group = Company.Filter<IntersectGroup>(g => g.IntersectID == id).FirstOrDefault();
        //        if (model == null) throw new NotFoundException("hierarchy");

        //        if (group != null)
        //            Company.Delete(group);

        //        Company.Delete(model);

        //        return null;//return jsonSuccess("Item successfully removed.", id.ToString(), "hierarchy", "delete", HttpStatusCode.OK, new { IntersectMapId = model.ID });
        //    }
        //    catch (BaseException ex)
        //    {
        //        return null; //return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        SendException(ex);
        //        return null; // return jsonException(ex, HttpStatusCode.InternalServerError);
        //    }
        //}

        [HttpGet, Route("hierarchy/{mapType}/{type}/{id:int}")]
        public JsonNetResult GetHierarchy(SystemObjects type, int id, PredicateType mapType)
        {
            //var sql = "EXEC GetHierarchyByMapType @type, @id, @mapType";

            //if (mapType == PredicateType.GroupHierarchy)
            //    sql = "EXEC GetGroupHierarchy @type, @id";

            //var results = Company.Query<HierarchyModel>(sql, new { type = type.ToString(), id = id, mapType = (int)mapType });

            return new JsonNetResult
            {
                Data = new { },// results,
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

            //var sql = @"select  [Object], 
            //            ObjectID, 
            //            ObjectTypeName + ': ' + TextPath as DisplayName,
            //            TextPath as Name,
            //            ObjectTypeName
            //            from cache.ObjectDetails d
            //            where ObjectID <> 0  and ObjectTypeName is not null
            //            and [Object] + '|' + cast(ObjectID as varchar(20)) not in
            //            (
	           //             select distinct n.objecttype + '|' + cast(n.objectid as varchar(20)) from intersectmap m
	           //             join intersectnode n on n.id = m.{0}
            //                {1}
	           //             where m.type = @mapType
            //            ) {2}";

            //var nodeId = "objectintersectnodeid";
            //if (model.IsAddingParent)
            //    nodeId = "subjectintersectnodeid";

            //switch(model.MapType)
            //{
            //    case PredicateType.TypeHierarchy:
            //        sql = string.Format(sql, nodeId, "", " and ObjectType = @type and ObjectTypeID = @id order by Name");
            //        break;
            //    case PredicateType.GroupHierarchy:
            //        sql = string.Format(sql, nodeId, "join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = @groupNumber", " and ObjectType = @type and ObjectTypeID = @id order by Name");
            //        break;
            //    default:
            //        sql += " order by Name";
            //        break;
            //}

            //var obj = Company.GetObjectDetail(model.Type, model.ID);
            //var allItems = Company.Query<dynamic>(sql, new { type = obj.Type, id = obj.TypeID, mapType = model.MapType, groupNumber = model.GroupNumber});
            //var itemList = allItems.ToList();

            //var intersectMap = Company.GetById<IntersectMap>(model.IntersectMapID);
            //var hierarchy = new List<HierarchyModel>();

            //if (intersectMap != null)
            //{
            //    var intersectNode = Company.GetById<IntersectNode>(intersectMap.SubjectIntersectNodeID);
            //    if (model.MapType == PredicateType.GroupHierarchy)
            //        hierarchy = Company.Query<HierarchyModel>("EXEC GetGroupHierarchy @type, @id", new { type = model.Type.ToString(), id = model.ID }).ToList();
            //    else
            //        hierarchy = Company.Query<HierarchyModel>("EXEC GetHierarchyByMapType @type, @id, @mapType", new { type = model.Type.ToString(), id = model.ID, mapType = model.MapType }).ToList();
            //}
            //else
            //{
            //    hierarchy.Add(new HierarchyModel() { Subject = model.Type.ToString(), SubjectID = model.ID });
            //}

            //foreach (dynamic d in allItems)
            //{
            //    switch(model.MapType)
            //    {
            //        case PredicateType.TypeHierarchy:
            //        case PredicateType.ParentChildHierarchy:
            //            var t = hierarchy.Where(r => r.Object == d.Object && r.ObjectID == d.ObjectID).FirstOrDefault();
            //            var t2 = hierarchy.Where(r => r.Subject == d.Object && r.SubjectID == d.ObjectID).FirstOrDefault();

            //            if (t != null || t2 != null)
            //                itemList.Remove(d);
            //            break;
            //        case PredicateType.GroupHierarchy:
            //            if (d.Object == model.Type.ToString() && d.ObjectID == model.ID)
            //                itemList.Remove(d);
            //            var g = hierarchy.Where(r => r.Object == d.Object && r.ObjectID == d.ObjectID && r.GroupNumber == model.GroupNumber).FirstOrDefault();
            //            var g2 = hierarchy.Where(r => r.Subject == d.Object && r.SubjectID == d.ObjectID && r.GroupNumber == model.GroupNumber).FirstOrDefault();

            //            if (g != null || g2 != null)
            //                itemList.Remove(d);
            //            break;
            //    }
            //}

            return new JsonNetResult
            {
                Data = null,//itemList,
                Formatting = Formatting.None
            };
        }

        #endregion Hierarchy

        //void processSourceLevel(List<SourcesToObjectModel> list, int id)
        //{

        //    var level = list.Single(i => i.ID == id && i.Type == "S").Level + 1;

        //    list.Where(i => i.ID == id && i.Type == "O").ToList().ForEach(i => {
        //        i.Level = level;
        //        processSourceLevel(list, i.O, i.OID, level);
        //    });
        //}
        //void processSourceLevel(List<SourcesToObjectModel> list, string obj, int objID, int level)
        //{
        //    list.Where(i => i.O == obj && i.OID == objID && i.Type == "S" && i.Level == 0).ToList().ForEach(i => {
        //        i.Level = level;
        //        processSourceLevel(list, i.ID);
        //    });
        //}


        //public DiagramModel TraverseDiagram(DiagramModel model, JsonNodeItem start)
        //{
        //    var diagram = new DiagramModel();
        //    diagram.nodes.Add(start);

        //    //links to the right
        //    var links = model.links.Where(l => l.from == start.key).ToList();

        //    links.ForEach(l =>
        //    {
        //        diagram.links.Add(l);
        //        var node = model.nodes.Where(i => i.key == l.to).SingleOrDefault();
        //        if (node == null)
        //            return;

        //        var k = TraverseDiagram(model, node);
        //        diagram.nodes.AddRange(k.nodes);
        //        diagram.links.AddRange(k.links);
        //    });

        //    return diagram;
        //}

        //public DiagramModel MergeDiagram(DiagramModel model)
        //{
        //    var leadingNodes = model.nodes.Where(n => !model.links.Any(l => l.to == n.key)).ToList();
        //    var diagrams = new List<DiagramModel>();

        //    //get discrete diagrams
        //    leadingNodes.ForEach(n =>
        //    {
        //        var diagram = TraverseDiagram(model, n);
        //        diagrams.Add(diagram);

        //    });

        //    //pick the biggest
        //    var mainDiagram = diagrams.OrderByDescending(d => d.nodes.Count).FirstOrDefault();

        //    //now merge the smaller diagrams into the main one if possible
        //    foreach (DiagramModel dgm in diagrams)
        //    {
        //        if (dgm == mainDiagram)
        //            continue;

        //        var nodeList = dgm.nodes.OrderByDescending(n => n.level);

        //        foreach (JsonNodeItem n in nodeList)
        //        {

        //            var node = mainDiagram.nodes.OrderBy(k => k.level).Where(k => k.obj == n.obj && k.objid == n.objid).FirstOrDefault();
        //            if (node == null)
        //                continue;
        //            else
        //            {
        //                var leftLinks = dgm.links.Where(l => l.to == n.key);
        //                var rightLinks = dgm.links.Where(l => l.from == n.key);

        //                var nodeExists = false;

        //                if (mainDiagram.nodes.Any(k => k.key == n.key))
        //                {
        //                    //make sure we don't delete this node later
        //                    nodeExists = true;
        //                }

        //                //point affected links to mainDiagram node
        //                foreach (JsonLinkItem l in leftLinks)
        //                    l.to = node.key;
        //                foreach (JsonLinkItem l in rightLinks)
        //                    l.from = node.key;

        //                if (!nodeExists)
        //                    model.nodes.Remove(n);
        //            }
        //        }
        //    }

        //    return model;
        //}

        //[HttpGet, Route("{type}/{id:int}/sources")]
        //public JsonNetResult GetSourcesByObject(SystemObjects type, int id)
        //{
        //    var list = Company.Query<SourcesToObjectModel>(@"exec GetLineageDiagram @type, @id", new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true }, id }).ToList();

        //    list.Where(i => i.Level == 1).ToList().ForEach(i =>
        //    {
        //        processSourceLevel(list, i.ID); //assumes type is "O"
        //    });

        //    var model = new DiagramModel();

        //    Func<string, int, string, int> getTotalSourceRules = delegate (string obj, int objID, string currentType)
        //    {
        //        return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawSourceRuleCount);
        //    };

        //    Func<string, int, string, int> getTotalMappingRules = delegate (string obj, int objID, string currentType)
        //    {
        //        return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawMappingRuleCount);
        //    };

        //    var IDs = list.Select(i => i.ID).Distinct().ToList();
        //    IDs.ForEach(m =>
        //    {
        //        var s = list.Single(i => i.ID == m && i.Type == "S");
        //        var sKey = $"{s.Level}{s.O}{s.OID}";
        //        if (!model.nodes.Any(i => i.key == sKey))
        //            model.nodes.Add(new JsonNodeItem { key = sKey, level = s.Level, obj = s.O, objid = s.OID, name = s.ObjectName, type = s.TypeName, objecttype = s.ObjectType, objecttypeid = s.ObjectTypeID, back = s.BackColor, fore = s.ForeColor, intersectMapId = s.ID, intersectId = s.IntersectID, mappingRuleCount = s.RawMappingRuleCount, challengeCount = s.ChallengeCount, openEventCount = s.OpenEventCount, openIssueCount = s.OpenIssueCount }); //, sourceRuleCount = getTotal(s.O, s.OID, s.Type)

        //        var o = list.Single(i => i.ID == m && i.Type == "O");
        //        var oKey = $"{o.Level}{o.O}{o.OID}";
        //        if (!model.nodes.Any(i => i.key == oKey))
        //            model.nodes.Add(new JsonNodeItem { key = oKey, level = o.Level, obj = o.O, objid = o.OID, name = o.ObjectName, type = o.TypeName, objecttype = o.ObjectType, objecttypeid = o.ObjectTypeID, back = o.BackColor, fore = o.ForeColor, intersectMapId = o.ID, intersectId = o.IntersectID, sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type), mappingRuleCount = o.RawMappingRuleCount, challengeCount = o.ChallengeCount, openEventCount = o.OpenEventCount, openIssueCount = o.OpenIssueCount });
        //        else
        //            model.nodes.First(i => i.key == oKey).sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type);

        //        if (model.links.Any(i => i.from == sKey && i.to == oKey))
        //        {
        //            var existingLink = model.links.Single(i => i.from == sKey && i.to == oKey);
        //            existingLink.text += $", {s.Predicate}";
        //        }
        //        else
        //        {
        //            model.links.Add(new JsonLinkItem { id = s.ID, intersectTypeId = s.IntersectTypeID, from = sKey, to = oKey, text = s.Predicate, predicateId = s.PredicateID, mappingRuleCount = s.LinkMappingRuleCount, transformationCount = s.LinkTransformationCount });
        //        }
        //    });

        //    model = MergeDiagram(model);

        //    return new JsonNetResult
        //    {
        //        Data = new { model.nodes, model.links },
        //        Formatting = Formatting.None
        //    };
        //}

        [HttpGet]
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

//        [Obsolete, HttpPost, Route("sources")]
//        public JsonNetResult LineageSourcePost(SourcePostModel models)
//        {
//            var message = "";
//            var success = false;

//            models.Adds.ForEach(model =>
//            {
//                #region 
//                if (string.IsNullOrEmpty(model.Focal) || model.FocalID <= 0)
//                {
//                    message += $"The Target, or current object, you provided is invalid.";
//                }
//                else
//                {
//                    if (string.IsNullOrEmpty(model.Subject) || model.SubjectID <= 0)
//                    {
//                        message += $"The Subject you provided is invalid.";
//                    }
//                    else
//                    {
//                        if (string.IsNullOrEmpty(model.Object) || model.ObjectID <= 0)
//                        {
//                            message += $"The Object you provided is invalid.";
//                        }
//                        else
//                        {
//                            if (model.Subject == model.Object && model.SubjectID == model.ObjectID)
//                            {
//                                message += $"A source may not map to itself directly.";
//                            }
//                            else
//                            {
//                                var predicate = Company.GetById<Predicate>(model.PredicateID);

//                                if (predicate.Type == PredicateType.Lineage)
//                                {
//                                    if ($"{model.Focal}{model.FocalID}" != $"{model.Subject}{model.SubjectID}")
//                                        Company.AddIntersect(model.Focal, model.FocalID, model.Subject, model.SubjectID, IntersectClassification.Normal, null, null);

//                                    if ($"{model.Focal}{model.FocalID}" != $"{model.Object}{model.ObjectID}")
//                                        Company.AddIntersect(model.Focal, model.FocalID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
//                                }


//                                Company.AddIntersect(model.Subject, model.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
//                                var intersect = Company.Query<IntersectLookupModel>(@"select top 1 
//S.IntersectID,
//S.ID as SubjectNodeID, S.[ObjectType] as Subject, S.ObjectID as SubjectID,
//O.ID as ObjectNodeID, O.[ObjectType] as [Object], O.ObjectID 
//from [IntersectNode] S 
//inner join IntersectNode O on O.IntersectID = S.IntersectID 
//and S.[ObjectType] = @s and S.ObjectID = @sid 
//and O.[ObjectType] = @o and O.ObjectID = @oid",
//            new { s = new Dapper.DbString { Value = model.Subject, IsAnsi = true }, sid = model.SubjectID, o = new Dapper.DbString { Value = model.Object, IsAnsi = true }, oid = model.ObjectID }
//            ).SingleOrDefault();
//                                if (intersect != null)
//                                {
//                                    var existingSourceRecordCount = Company.Query<int>(
//                                        "select count(1) from IntersectMap where SubjectIntersectNodeID = @s and ObjectIntersectNodeID = @o and PredicateID = @p",
//                                        new { s = intersect.SubjectNodeID, o = intersect.ObjectNodeID, p = model.PredicateID }
//                                    ).Single();

//                                    if (existingSourceRecordCount <= 0)
//                                    {
//                                        // If we got here, we are all good.

//                                        var intersectMap = new IntersectMap
//                                        {
//                                            ObjectIntersectNodeID = intersect.ObjectNodeID,// objectIntersectNode.ID,
//                                            PredicateID = model.PredicateID,
//                                            SubjectIntersectNodeID = intersect.SubjectNodeID,// subjectIntersectNode.ID,
//                                            Type = PredicateType.Lineage
//                                        };
//                                        Company.Add<IntersectMap>(intersectMap);
//                                    }
//                                }
//                                else
//                                {
//                                    message += $"The Subject or Object did not match up with the Relationship you provided.";
//                                }
//                            }
//                        }
//                    }
//                }
//                #endregion
//            });

//            models.Deletes.ForEach(model =>
//            {
//                #region 
//                if (model.IntersectMapID <= 0)
//                {
//                    message = $"The source ID ({model.IntersectMapID}) is invalid.";
//                }
//                else
//                {
//                    var o = Company.GetById<IntersectMap>(model.IntersectMapID);
//                    if (o == null)
//                    {
//                        message += $"The source with ID ({model.IntersectMapID}) could not be found.";
//                    }
//                    else
//                    {
//                        if (!Company.HasPermission(model.Focal, model.FocalID, Claim.Delete, ClaimObject.Relationship))
//                        {
//                            message = FormInfo.Permisions_Error_Delete;
//                        }
//                        else
//                        {
//                            Company.Delete<IntersectMap>(o);
//                        }
//                    }
//                }
//                #endregion
//            });

//            models.Edits.ForEach(model =>
//            {
//                #region 
//                if (model.IntersectMapID <= 0)
//                {
//                    message += $"The intersect map ID ({model.IntersectMapID}) is invalid.";
//                }
//                else
//                {
//                    var o = Company.GetById<IntersectMap>(model.IntersectMapID);
//                    if (o == null)
//                    {
//                        message += $"The intersect map record with ID ({model.IntersectMapID}) cound not be found.";
//                    }
//                    else
//                    {
//                        o.PredicateID = model.PredicateID;
//                        Company.Update(o);
//                    }
//                }
//                #endregion
//            });

//            success = string.IsNullOrEmpty(message);

//            if (string.IsNullOrEmpty(message))
//            {
//                message = "Successfully updated lineage.";
//            }

//            return new JsonNetResult
//            {
//                Data = new
//                {
//                    message = message,
//                    success = success
//                },
//                Formatting = Formatting.None
//            };
//        }

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
