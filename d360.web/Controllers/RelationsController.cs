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
            public MapType Type { get; set; }
            public int GroupNumber { get; set; }
            public string UID { get; set; }

        }

        public class HierarchyArtifactsModel
        {
            public int IntersectMapID { get; set; }
            public MapType MapType { get; set; }
            public SystemObjects Type { get; set; }
            public int ID { get; set; }
            public int GroupNumber { get; set; }
            public bool IsAddingParent { get; set; }
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
        public JsonNetResult Predicates()
        {
            var predicates = Company.Table<Predicate>().OrderBy(i => i.Name);
            var data = new List<dynamic>();

            predicates.ToList().ForEach(p =>
                {
                    data.Add(new
                    {
                        ID = p.ID,
                        Name = p.Name,
                        Inverse = p.Inverse,
                        Type = p.Type.GetName()
                    });
            });

            return new JsonNetResult
            {
                Data = data,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("sources/predicates")]
        public JsonNetResult GetPredicates()
        {
            var list = Company.Query<dynamic>(@"select ID as [value], Name as [text] from Predicate order by Name");
            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public JsonResult _IntersectTypes()
        {
            var models = Company.Query<IntersectTypeListViewModel>(
@"select    I.ID,
			S.ObjectType as Source,
			S.ObjectID as SourceID,
			SD.TextPath as SourceName,
			T.ObjectType as Target,
			T.ObjectID as TargetID,
			TD.TextPath as TargetName
from		IntersectType I
			inner join IntersectTypeNode S on S.IntersectTypeID = I.ID and S.[Order] = 1
			inner join IntersectTypeNode T on T.IntersectTypeID = I.ID and T.ID <> S.ID
			left join cache.ObjectDetails SD on SD.[Object] = S.ObjectType and SD.ObjectID = S.ObjectID
			left join cache.ObjectDetails TD on TD.[Object] = T.ObjectType and TD.ObjectID = T.ObjectID
order by	SD.Name,
			TD.Name");
            return Json(models, JsonRequestBehavior.AllowGet);
        }

        public JsonNetResult OptionsToRelate(SystemObjects type, int id)
        {
            #region SQL
            var sql = @"
select  distinct
        RT.IntersectTypeID,
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
		inner join [utility].[RelationshipTypes] RT on RT.SourceObjectType = O.[Type] and RT.SourceObjectID = O.[ID]
		inner join cache.[Object] SO on SO.[ObjectType] = RT.TargetObjectType and SO.ObjectTypeID = RT.TargetObjectID and SO.[Object] = @type and SO.ObjectID = @id
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

        public JsonNetResult PossibleRelationshipsBySource(string source, int id, string targetType, int targetTypeID)
        {
            var sql = "";

            if (targetType == "FusionAttributeType")
            {
                sql = @"select	D.[Object], D.ObjectID, F.Name + '.' + D.TextPath as Name, D.Url
from	cache.ObjectDetails D
		inner join FusionAttribute FA on D.[ObjectType] = @targetType and D.ObjectTypeID = @targetTypeID and FA.ID = D.ObjectID
		inner join Fusion F on F.ID = FA.FusionID
where	D.ObjectTypeID <> D.ObjectID 
        and D.ObjectTypeID <> 0
        and (D.[Object] + cast(D.ObjectID as varchar) <> @source + cast(@id as varchar))
        and not exists  (
					select	1 
					from	[cache].[Relationship] R 
					where	R.SourceObject = @source 
							and R.SourceObjectID = @id
							and R.TargetObject = D.[Object] 
							and R.TargetObjectID = D.ObjectID
					)
order by F.Name, D.TextPath";
            }
            else if (targetType == "Group" || targetType == "GroupType")
            {
                sql = @"select	D.[Object], D.ObjectID, D.TextPath as Name, D.Url
from	cache.ObjectDetails D
where	D.[ObjectType] = 'Group'
        and (D.[Object] + cast(D.ObjectID as varchar) <> @source + cast(@id as varchar))
        and not exists  (
					select	1 
					from	[cache].[Relationship] R 
					where	R.SourceObject = @source 
							and R.SourceObjectID = @id
							and R.TargetObject = D.[Object] 
							and R.TargetObjectID = D.ObjectID
					)
order by D.TextPath";
            }
            else if (targetType == "Resource" || targetType == "ResourceType")
            {
                sql = @"select	D.[Object], D.ObjectID, D.TextPath as Name, D.Url
from	cache.ObjectDetails D
where   D.[ObjectType] = 'ResourceType'
        and (D.[Object] + cast(D.ObjectID as varchar) <> @source + cast(@id as varchar))
        and not exists  (
					select	1 
					from	[cache].[Relationship] R 
					where	R.SourceObject = @source 
							and R.SourceObjectID = @id
							and R.TargetObject = D.[Object] 
							and R.TargetObjectID = D.ObjectID
					)
order by D.TextPath";
            }
            else
            {
                sql = @"select	D.[Object], D.ObjectID, D.TextPath as Name, D.Url
from	cache.ObjectDetails D
where	D.[ObjectType] = @targetType and D.ObjectTypeID = @targetTypeID 
        and D.ObjectTypeID <> D.ObjectID 
        and D.ObjectTypeID <> 0
        and (D.[Object] + cast(D.ObjectID as varchar) <> @source + cast(@id as varchar))
		and not exists (
						select	1 
						from	[cache].[Relationship] R 
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

        [ValidateHttpAntiForgeryToken, HttpPost]
        [Route("hierarchy/save")]
        public JsonNetResult SaveHierarchy(HierarchyPostModel model)
        {
            var message = "";

            if (string.IsNullOrEmpty(model.Subject) || model.SubjectID <= 0)
            {
                message = $"The Subject you provided is invalid.";
            }
            else if (string.IsNullOrEmpty(model.Object) || model.ObjectID <= 0)
            {
                message = $"The Object you provided is invalid.";
            }
            else if (model.Subject == model.Object && model.SubjectID == model.ObjectID)
            {
                message = $"A source may not map to itself directly.";
            }
            else if ((int)model.HierarchyType < 1)
            {
                message = $"The hierarchy type is invalid.";
            }

            Predicate predicate = null; //Company.GetById<Predicate>(model.PredicateID);
            if (model.HierarchyType == MapType.GroupHierarchy)
            {
                predicate = Company.Filter<Predicate>(i => i.Type == MapType.GroupHierarchy).FirstOrDefault();
            }
            else
            {
                predicate = Company.Filter<Predicate>(i => i.Type == MapType.TypeHierarchy).FirstOrDefault();
            }

            if (predicate == null)
            {
                message = "No predicate exists to fulfill this request.";
            }

            if (message == "")
            {
                if (model.IsAddingParent)
                {
                    int subid = model.SubjectID;
                    string subtype = model.Subject;
                    if (model.IntersectMapID > 0)
                    {
                        //first add a relationship between the old parent of the object and the new parent

                        var record = Company.Query<dynamic>("select n1.ObjectType as SubjectType, n1.ObjectID as SubjectID, n2.ObjectType, n2.ObjectID from intersectmap m join intersectnode n1 on n1.id = m.subjectintersectnodeid join intersectnode n2 on n2.id = m.objectintersectnodeid where m.id = @id", new { id = model.IntersectMapID }).Single();

                        subid = record.ObjectID;
                        subtype = record.ObjectType;

                        Company.AddRelationship(record.SubjectType, record.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
                        var intersect = Company.Query<IntersectLookupModel>(@"select top 1 
                                S.IntersectID,
                                S.ID as SubjectNodeID, S.[ObjectType] as Subject, S.ObjectID as SubjectID,
                                O.ID as ObjectNodeID, O.[ObjectType] as [Object], O.ObjectID 
                                from [IntersectNode] S 
                                inner join IntersectNode O on O.IntersectID = S.IntersectID 
                                and S.[ObjectType] = @s and S.ObjectID = @sid 
                                and O.[ObjectType] = @o and O.ObjectID = @oid",
                            new { s = record.SubjectType, sid = record.SubjectID, o = model.Object, oid = model.ObjectID }
                            ).SingleOrDefault();

                        if (intersect != null)
                        {
                            var existingSourceRecordCount = Company.Query<int>(
                                "select count(1) from IntersectMap where SubjectIntersectNodeID = @s and ObjectIntersectNodeID = @o and PredicateID = @p",
                                new { s = intersect.SubjectNodeID, o = intersect.ObjectNodeID, p = model.PredicateID }
                            ).Single();

                            if (existingSourceRecordCount <= 0)
                            {
                                var intersectMap = new IntersectMap
                                {
                                    ObjectIntersectNodeID = intersect.ObjectNodeID,// objectIntersectNode.ID,
                                    PredicateID = predicate.ID, //model.PredicateID,
                                    SubjectIntersectNodeID = intersect.SubjectNodeID,// subjectIntersectNode.ID,
                                    Type = model.HierarchyType
                                };
                                try
                                {
                                    Company.Add(intersectMap);
                                } catch (Exception ex)
                                {
                                    message = ex.Message;
                                }
                                

                                if (model.HierarchyType == MapType.GroupHierarchy && message == "")
                                {
                                    var intersectMapGroup = new IntersectMapGroup();
                                    intersectMapGroup.IntersectMapID = intersectMap.ID;
                                    if (model.GroupNumber > -1)
                                        intersectMapGroup.GroupNumber = model.GroupNumber;
                                    else
                                    {
                                        intersectMapGroup.GroupNumber = Company.Query<int>("select coalesce(max(groupnumber) + 1, 1) from intersectmapgroup").Single();
                                        model.GroupNumber = intersectMapGroup.GroupNumber;
                                    }
                                    
                                    try
                                    {
                                        Company.Add(intersectMapGroup);
                                    } catch (Exception ex)
                                    {
                                        message = ex.Message;
                                    }
                                }
                            }
                        }

                        try
                        {
                            var intersectMap = Company.GetById<IntersectMap>(model.IntersectMapID);
                            Company.Delete(intersectMap);
                        }
                        catch (Exception ex)
                        {
                            message = ex.Message;
                        }

                        if (model.HierarchyType == MapType.GroupHierarchy && model.GroupNumber > -1 && message == "")
                        {
                            try
                            {
                                var intersectMapGroup = Company.IntersectMapGroups.Where(g => g.IntersectMapID == model.IntersectMapID && g.GroupNumber == model.GroupNumber).FirstOrDefault();

                                if (intersectMapGroup != null)
                                {
                                    Company.Delete(intersectMapGroup);
                                }
                            }
                            catch (Exception ex)
                            {
                                message = ex.Message;
                            }
                        }
                    }
                    
                    //swap subject/object and add the relationship
                    model.Subject = model.Object;
                    model.SubjectID = model.ObjectID;
                    model.Object = subtype;
                    model.ObjectID = subid;
                }

                if (message == "")
                {
                    Company.AddRelationship(model.Subject, model.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
                    var intersect2 = Company.Query<IntersectLookupModel>(@"select top 1 
                                S.IntersectID,
                                S.ID as SubjectNodeID, S.[ObjectType] as Subject, S.ObjectID as SubjectID,
                                O.ID as ObjectNodeID, O.[ObjectType] as [Object], O.ObjectID 
                                from [IntersectNode] S 
                                inner join IntersectNode O on O.IntersectID = S.IntersectID 
                                and S.[ObjectType] = @s and S.ObjectID = @sid 
                                and O.[ObjectType] = @o and O.ObjectID = @oid",
                        new { s = model.Subject, sid = model.SubjectID, o = model.Object, oid = model.ObjectID }
                        ).SingleOrDefault();

                    if (intersect2 != null)
                    {
                        var existingSourceRecordCount = Company.Query<int>(
                            "select count(1) from IntersectMap where SubjectIntersectNodeID = @s and ObjectIntersectNodeID = @o and PredicateID = @p",
                            new { s = intersect2.SubjectNodeID, o = intersect2.ObjectNodeID, p = model.PredicateID }
                        ).Single();

                        if (existingSourceRecordCount <= 0)
                        {
                            var intersectMap = new IntersectMap
                            {
                                ObjectIntersectNodeID = intersect2.ObjectNodeID,// objectIntersectNode.ID,
                                PredicateID = predicate.ID,
                                SubjectIntersectNodeID = intersect2.SubjectNodeID,// subjectIntersectNode.ID,
                                Type = model.HierarchyType
                            };

                            try
                            {
                                Company.Add(intersectMap);
                            }
                            catch (Exception ex)
                            {
                                message = ex.Message;
                            }

                            if (model.HierarchyType == MapType.GroupHierarchy && message == "")
                            {
                                try
                                {
                                    var intersectMapGroup = new IntersectMapGroup();
                                    intersectMapGroup.IntersectMapID = intersectMap.ID;
                                    if (model.GroupNumber > -1)
                                        intersectMapGroup.GroupNumber = model.GroupNumber;
                                    else
                                        intersectMapGroup.GroupNumber = Company.Query<int>("select coalesce(max(groupnumber) + 1, 1) from intersectmapgroup").Single();
                                    Company.Add(intersectMapGroup);
                                }
                                catch (Exception ex)
                                {
                                    message = ex.Message;
                                }
                            }

                        }
                    }
                }
            }


            if (string.IsNullOrEmpty(message))
            {
                return new JsonNetResult
                {
                    Data = new { type = "success", title = "Success", message = "Updated structure" },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else
            {
                return new JsonNetResult
                {
                    Data = new { type = "error", title = "An error occured", message = message },
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
        }

        [ValidateHttpAntiForgeryToken, HttpPost]
        [Route("hierarchy/edit")]
        public JsonResult EditHierarchy(HierarchyPostModel model)
        {
            var intersectMap = Company.GetById<IntersectMap>(model.IntersectMapID);

            if (intersectMap == null)
                return null;

            intersectMap.PredicateID = model.PredicateID;
            Company.Update(intersectMap);

            return null;
            
        }

        [HttpDelete]
        [Route("hierarchy/delete/{id:int}")]
        public JsonResult DeleteHierarchy(int id)
        {
            
            try
            {
                var model = Company.GetById<IntersectMap>(id);
                var group = Company.IntersectMapGroups.Where(g => g.IntersectMapID == id).FirstOrDefault();
                if (model == null) throw new NotFoundException("hierarchy");

                Company.Delete(model);

                if (group != null)
                    Company.Delete(group);

                return null;//return jsonSuccess("Item successfully removed.", id.ToString(), "hierarchy", "delete", HttpStatusCode.OK, new { IntersectMapId = model.ID });
            }
            catch (BaseException ex)
            {
                return null; //return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return null; // return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet, Route("hierarchy/{mapType}/{type}/{id:int}")]
        public JsonNetResult GetHierarchy(SystemObjects type, int id, MapType mapType)
        {
            var sql = "EXEC GetHierarchyByMapType @type, @id, @mapType";

            if (mapType == MapType.GroupHierarchy)
                sql = "EXEC GetGroupHierarchy @type, @id";

            var results = Company.Query<HierarchyModel>(sql, new { type = type.ToString(), id = id, mapType = (int)mapType });

            return new JsonNetResult
            {
                Data = results,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, Route("hierarchy/artifacts")]
        public JsonNetResult GetHierarchyArtifacts(HierarchyArtifactsModel model)
        {

            var sql = @"select  [Object], 
                        ObjectID, 
                        ObjectTypeName + ': ' + TextPath as DisplayName,
                        TextPath as Name,
                        ObjectTypeName
                        from cache.ObjectDetails d
                        where ObjectID <> 0  and ObjectTypeName is not null
                        and [Object] + '|' + cast(ObjectID as varchar(20)) not in
                        (
	                        select distinct n.objecttype + '|' + cast(n.objectid as varchar(20)) from intersectmap m
	                        join intersectnode n on n.id = m.{0}
                            {1}
	                        where m.type = @mapType
                        ) {2}";

            var nodeId = "objectintersectnodeid";
            if (model.IsAddingParent)
                nodeId = "subjectintersectnodeid";

            switch(model.MapType)
            {
                case MapType.TypeHierarchy:
                    sql = string.Format(sql, nodeId, "", " and ObjectType = @type and ObjectTypeID = @id order by Name");
                    break;
                case MapType.GroupHierarchy:
                    sql = string.Format(sql, nodeId, "join intersectmapgroup g on g.intersectmapid = m.id and g.groupnumber = @groupNumber", " and ObjectType = @type and ObjectTypeID = @id order by Name");
                    break;
                default:
                    sql += " order by Name";
                    break;
            }

            var obj = Company.GetObjectDetail(model.Type, model.ID);
            var allItems = Company.Query<dynamic>(sql, new { type = obj.Type, id = obj.TypeID, mapType = model.MapType, groupNumber = model.GroupNumber});
            var itemList = allItems.ToList();

            var intersectMap = Company.GetById<IntersectMap>(model.IntersectMapID);
            var hierarchy = new List<HierarchyModel>();

            if (intersectMap != null)
            {
                var intersectNode = Company.GetById<IntersectNode>(intersectMap.SubjectIntersectNodeID);
                if (model.MapType == MapType.GroupHierarchy)
                    hierarchy = Company.Query<HierarchyModel>("EXEC GetGroupHierarchy @type, @id", new { type = model.Type.ToString(), id = model.ID }).ToList();
                else
                    hierarchy = Company.Query<HierarchyModel>("EXEC GetHierarchyByMapType @type, @id, @mapType", new { type = model.Type.ToString(), id = model.ID, mapType = model.MapType }).ToList();
            }
            else
            {
                hierarchy.Add(new HierarchyModel() { Subject = model.Type.ToString(), SubjectID = model.ID });
            }

            foreach (dynamic d in allItems)
            {
                switch(model.MapType)
                {
                    case MapType.TypeHierarchy:
                    case MapType.ParentChildHierarchy:
                        var t = hierarchy.Where(r => r.Object == d.Object && r.ObjectID == d.ObjectID).FirstOrDefault();
                        var t2 = hierarchy.Where(r => r.Subject == d.Object && r.SubjectID == d.ObjectID).FirstOrDefault();

                        if (t != null || t2 != null)
                            itemList.Remove(d);
                        break;
                    case MapType.GroupHierarchy:
                        if (d.Object == model.Type.ToString() && d.ObjectID == model.ID)
                            itemList.Remove(d);
                        var g = hierarchy.Where(r => r.Object == d.Object && r.ObjectID == d.ObjectID && r.GroupNumber == model.GroupNumber).FirstOrDefault();
                        var g2 = hierarchy.Where(r => r.Subject == d.Object && r.SubjectID == d.ObjectID && r.GroupNumber == model.GroupNumber).FirstOrDefault();

                        if (g != null || g2 != null)
                            itemList.Remove(d);
                        break;
                }
            }

            return new JsonNetResult
            {
                Data = itemList,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion Hierarchy

        void processSourceLevel(List<SourcesToObjectModel> list, int id)
        {

            var level = list.Single(i => i.ID == id && i.Type == "S").Level + 1;

            list.Where(i => i.ID == id && i.Type == "O").ToList().ForEach(i => {
                i.Level = level;
                processSourceLevel(list, i.O, i.OID, level);
            });
        }
        void processSourceLevel(List<SourcesToObjectModel> list, string obj, int objID, int level)
        {
            list.Where(i => i.O == obj && i.OID == objID && i.Type == "S" && i.Level == 0).ToList().ForEach(i => {
                i.Level = level;
                processSourceLevel(list, i.ID);
            });
        }


        public DiagramModel TraverseDiagram(DiagramModel model, JsonNodeItem start)
        {
            var diagram = new DiagramModel();
            diagram.nodes.Add(start);

            //links to the right
            var links = model.links.Where(l => l.from == start.key).ToList();

            links.ForEach(l =>
            {
                diagram.links.Add(l);
                var node = model.nodes.Where(i => i.key == l.to).SingleOrDefault();
                if (node == null)
                    return;

                var k = TraverseDiagram(model, node);
                diagram.nodes.AddRange(k.nodes);
                diagram.links.AddRange(k.links);
            });

            return diagram;
        }

        public DiagramModel MergeDiagram(DiagramModel model)
        {
            var leadingNodes = model.nodes.Where(n => !model.links.Any(l => l.to == n.key)).ToList();
            var diagrams = new List<DiagramModel>();

            //get discrete diagrams
            leadingNodes.ForEach(n =>
            {
                var diagram = TraverseDiagram(model, n);
                diagrams.Add(diagram);

            });

            //pick the biggest
            var mainDiagram = diagrams.OrderByDescending(d => d.nodes.Count).FirstOrDefault();

            //now merge the smaller diagrams into the main one if possible
            foreach (DiagramModel dgm in diagrams)
            {
                if (dgm == mainDiagram)
                    continue;

                var nodeList = dgm.nodes.OrderByDescending(n => n.level);

                foreach (JsonNodeItem n in nodeList)
                {

                    var node = mainDiagram.nodes.OrderBy(k => k.level).Where(k => k.obj == n.obj && k.objid == n.objid).FirstOrDefault();
                    if (node == null)
                        continue;
                    else
                    {
                        var leftLinks = dgm.links.Where(l => l.to == n.key);
                        var rightLinks = dgm.links.Where(l => l.from == n.key);

                        var nodeExists = false;

                        if (mainDiagram.nodes.Any(k => k.key == n.key))
                        {
                            //make sure we don't delete this node later
                            nodeExists = true;
                        }

                        //point affected links to mainDiagram node
                        foreach (JsonLinkItem l in leftLinks)
                            l.to = node.key;
                        foreach (JsonLinkItem l in rightLinks)
                            l.from = node.key;

                        if (!nodeExists)
                            model.nodes.Remove(n);
                    }
                }
            }

            return model;
        }

        [HttpGet, Route("{type}/{id:int}/sources")]
        public JsonNetResult GetSourcesByObject(SystemObjects type, int id)
        {
            #region Legacy SQL

//            var sql = @"
//select	distinct
//		R.IntersectID,
//		M.ID,
//		M.SubjectIntersectNodeID,
//		R.SourceTypeName,
//		R.SourceObjectName,
//		R.SourceObject,
//		R.SourceObjectID,
//		SD.[IconBackColor] as SourceIconBackColor,
//		SD.[IconForeColor] as SourceIconForeColor,
//        M.ObjectIntersectNodeID,
//		R.TargetTypeName,
//		R.TargetObjectName,
//		R.TargetObject,
//		R.TargetObjectID,
//		TD.[IconBackColor] as TargetIconBackColor,
//		TD.[IconForeColor] as TargetIconForeColor,
//        M.PredicateID,
//		P.Name as Predicate
//from	IntersectMap M
//		inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
//		inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
//		inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
//        inner join Predicate P on P.ID = M.PredicateID
//		inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
//		inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
//union
//select	distinct
//		R.IntersectID,
//		M.ID,
//		M.SubjectIntersectNodeID,
//		R.SourceTypeName,
//		R.SourceObjectName,
//		R.SourceObject,
//		R.SourceObjectID,
//		SD.[IconBackColor] as SourceIconBackColor,
//		SD.[IconForeColor] as SourceIconForeColor,
//		M.ObjectIntersectNodeID,
//		R.TargetTypeName,
//		R.TargetObjectName,
//		R.TargetObject,
//		R.TargetObjectID,
//		TD.[IconBackColor] as TargetIconBackColor,
//		TD.[IconForeColor] as TargetIconForeColor,
//		M.PredicateID,
//		P.Name as Predicate
//from	IntersectMap M
//		inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
//		inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
//		inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
//		inner join Predicate P on P.ID = M.PredicateID
//union
//select	distinct
//		R.IntersectID,
//		M.ID,
//		M.SubjectIntersectNodeID,
//		R.SourceTypeName,
//		R.SourceObjectName,
//		R.SourceObject,
//		R.SourceObjectID,
//		SD.[IconBackColor] as SourceIconBackColor,
//		SD.[IconForeColor] as SourceIconForeColor,
//		M.ObjectIntersectNodeID,
//		R.TargetTypeName,
//		R.TargetObjectName,
//		R.TargetObject,
//		R.TargetObjectID,
//		TD.[IconBackColor] as TargetIconBackColor,
//		TD.[IconForeColor] as TargetIconForeColor,
//		M.PredicateID,
//		P.Name as Predicate
//from	IntersectMap M
//		inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
//		inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
//		inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
//		inner join Predicate P on P.ID = M.PredicateID";

            #endregion

            #region SQL

            var sql1 = @"
                        declare @tbl table	(
					                        IntersectID int, IntersectTypeID int, ID int, 
					                        SubjectNodeID int, SubjectTypeName nvarchar(1000), SourceType varchar(50), SourceTypeID int, SubjectObjectName nvarchar(1000), Subject varchar(50), SubjectID int, SubjectBackColor varchar(10), SubjectForeColor varchar(10),  
					                        ObjectNodeID int, ObjectTypeName nvarchar(1000), ObjectType varchar(50), ObjectTypeID int, ObjectObjectName nvarchar(1000), Object varchar(50), ObjectID int, ObjectBackColor varchar(10), ObjectForeColor varchar(10),
					                        PredicateID int, Predicate nvarchar(250), MappingRuleCount int
					                        )
                        insert into @tbl
	                        select	distinct
			                        R.IntersectID,
									R.IntersectTypeID,
			                        M.ID,
			                        M.SubjectIntersectNodeID,
			                        R.SourceTypeName,
			                        R.SourceType,
			                        R.SourceTypeID,
			                        R.SourceObjectName,
			                        R.SourceObject,
			                        R.SourceObjectID,
			                        SD.[IconBackColor] as SourceIconBackColor,
			                        SD.[IconForeColor] as SourceIconForeColor,
			                        M.ObjectIntersectNodeID,
			                        R.TargetTypeName,
			                        R.TargetType,
			                        R.TargetTypeID,
			                        R.TargetObjectName,
			                        R.TargetObject,
			                        R.TargetObjectID,
			                        TD.[IconBackColor] as TargetIconBackColor,
			                        TD.[IconForeColor] as TargetIconForeColor,
			                        M.PredicateID,
			                        P.Name as Predicate,
			                        0
	                        from	IntersectMap M
			                        inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
			                        inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			                        inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			                        inner join Predicate P on P.ID = M.PredicateID
			                        inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
			                        inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
	                        union
	                        select	distinct
			                        R.IntersectID,
									R.IntersectTypeID,
			                        M.ID,
			                        M.SubjectIntersectNodeID,
			                        R.SourceTypeName,
			                        R.SourceType,
			                        R.SourceTypeID,
			                        R.SourceObjectName,
			                        R.SourceObject,
			                        R.SourceObjectID,
			                        SD.[IconBackColor] as SourceIconBackColor,
			                        SD.[IconForeColor] as SourceIconForeColor,
			                        M.ObjectIntersectNodeID,
			                        R.TargetTypeName,
			                        R.TargetType,
			                        R.TargetTypeID,
			                        R.TargetObjectName,
			                        R.TargetObject,
			                        R.TargetObjectID,
			                        TD.[IconBackColor] as TargetIconBackColor,
			                        TD.[IconForeColor] as TargetIconForeColor,
			                        M.PredicateID,
			                        P.Name as Predicate,
			                        0
	                        from	IntersectMap M
			                        inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
			                        inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			                        inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			                        inner join Predicate P on P.ID = M.PredicateID
	                        union
	                        select	distinct
			                        R.IntersectID,
									R.IntersectTypeID,
			                        M.ID,
			                        M.SubjectIntersectNodeID,
			                        R.SourceTypeName,
			                        R.SourceType,
			                        R.SourceTypeID,
			                        R.SourceObjectName,
			                        R.SourceObject,
			                        R.SourceObjectID,
			                        SD.[IconBackColor] as SourceIconBackColor,
			                        SD.[IconForeColor] as SourceIconForeColor,
			                        M.ObjectIntersectNodeID,
			                        R.TargetTypeName,
			                        R.TargetType,
			                        R.TargetTypeID,
			                        R.TargetObjectName,
			                        R.TargetObject,
			                        R.TargetObjectID,
			                        TD.[IconBackColor] as TargetIconBackColor,
			                        TD.[IconForeColor] as TargetIconForeColor,
			                        M.PredicateID,
			                        P.Name as Predicate,
			                        0
	                        from	IntersectMap M
			                        inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
			                        inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			                        inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			                        inner join Predicate P on P.ID = M.PredicateID

                        update r
                        set r.mappingrulecount = l.[Count]
                        from @tbl r
                        cross apply (
				                        select count(1) as [Count]
				                        from SourceTargetRule
				                        where FocalObjectID = @id and FocalObject = @type and SourceObject = r.Subject and SourceObjectID = r.SubjectID and TargetObject = r.Object and TargetObjectID = r.ObjectID
			                        ) l;

                        declare @h table	(
					                        ID int, [Type] varchar(1), IsStart bit, IsEnd bit,
					                        [Level] int, NodeID int, TypeName nvarchar(1000), [ObjectType] varchar(50), ObjectTypeID int, ObjectName nvarchar(1000), O varchar(50), OID int, BackColor varchar(10), ForeColor varchar(10),
					                        IntersectID int, IntersectTypeID int,  PredicateID int, Predicate nvarchar(250),
					                        RawSourceRuleCount int, RawMappingRuleCount int, LinkMappingRuleCount int, ChallengeCount int
					                        )

                        insert into @h
	                        select	ID, 'S', 0, 0, 0, SubjectNodeID, SubjectTypeName, SourceType, SourceTypeID, SubjectObjectName, Subject, SubjectID, SubjectBackColor, SubjectForeColor, IntersectID, IntersectTypeID, PredicateID, Predicate, R.[Count], M.[Count], S.MappingRuleCount, C.[Count]
	                        from	@tbl S
			                        cross apply (
						                        select	count(1) as [Count]
						                        from	SourceRule
						                        where	AppliesToObject = @type and AppliesToObjectID = @id and Object = S.Subject and ObjectID = S.SubjectID
						                        ) R
			                        cross apply (
						                            select count(1) as [Count]
						                            from SourceTargetRule
						                            where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Subject and SourceObjectID = S.SubjectID and TargetObject = S.Subject and TargetObjectID = S.SubjectID
						                        ) M
									cross apply (
													select count(1) as [Count]     
													from Workflow W            			                          
													where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column(""S.SubjectID"")]') = 1 and W.DateCompleted is null   
												) C
                        insert into @h

                            select ID, 'O', 0, 0, 0, ObjectNodeID, ObjectTypeName, ObjectType, ObjectTypeID, ObjectObjectName, Object, ObjectID, ObjectBackColor, ObjectForeColor, IntersectID, IntersectTypeID, PredicateID, Predicate, R.[Count], M.[Count], S.MappingRuleCount, C.[Count]

                            from @tbl S

                                    cross apply(
                                                select  count(1) as [Count]

                                                from SourceRule

                                                where AppliesToObject = @type and AppliesToObjectID = @id and Object = S.Object and ObjectID = S.ObjectID
                                                ) R
                                    cross apply(
                                                    select count(1) as [Count]

                                                    from SourceTargetRule

                                                    where FocalObjectID = @id and FocalObject = @type and SourceObject = S.Object and SourceObjectID = S.ObjectID and TargetObject = S.Object and TargetObjectID = S.ObjectID
                                                ) M
                                    cross apply(
                                                    select count(1) as [Count]

                                                    from Workflow W

                                                    where W.WorkflowType = 4 and W.Data.exist('/fields/ArtifactID[text() = sql:column(""S.ObjectID"")]') = 1 and W.DateCompleted is null
                                                ) C
                        update  T
                        set     T.[Level] = 1,
		                        T.IsStart = 1
                        from @h T

                                left
                        join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'O'
                        where T.[Type] = 'S'

                                and S.ID is null

                        update T
                        set T.IsEnd = 1
                        from @h T
                                left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'S'
                        where T.[Type] = 'O'

                                and S.ID is null

                        select* from @h";

            #endregion

            var list = Company.Query<SourcesToObjectModel>(sql1, new { type = new Dapper.DbString { Value = type.ToString(), IsAnsi = true } , id }).ToList();

            list.Where(i => i.Level == 1).ToList().ForEach(i =>
            {
                processSourceLevel(list, i.ID); //assumes type is "O"
            });

            //foreach(var o in list.Where(o => o.Type == "O"))
            //{
            //    o.SourceRuleCount = list.Where(s => s.O == o.O && s.OID == o.OID && s.Type == "S").Sum(s => s.RawSourceRuleCount);
            //}

            var model = new DiagramModel();

            Func<string, int, string, int> getTotalSourceRules = delegate(string obj, int objID, string currentType) {
                return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawSourceRuleCount);
            };

            Func<string, int, string, int> getTotalMappingRules = delegate (string obj, int objID, string currentType) {
                return list.Where(i => i.O == obj && i.OID == objID && i.Type == "O").Sum(i => i.RawMappingRuleCount);
            };

            var IDs = list.Select(i => i.ID).Distinct().ToList();
            IDs.ForEach(m =>
            {
                var s = list.Single(i => i.ID == m && i.Type == "S");
                var sKey = $"{s.Level}{s.O}{s.OID}";
                if (!model.nodes.Any(i => i.key == sKey))
                    model.nodes.Add(new JsonNodeItem { key = sKey, level = s.Level, obj = s.O, objid = s.OID, name = s.ObjectName, type = s.TypeName, objecttype = s.ObjectType, objecttypeid = s.ObjectTypeID, back = s.BackColor, fore = s.ForeColor, intersectMapId = s.ID, intersectId = s.IntersectID, mappingRuleCount = s.RawMappingRuleCount, challengeCount = s.ChallengeCount }); //, sourceRuleCount = getTotal(s.O, s.OID, s.Type)
                //else
                //    model.nodes.First(i => i.key == sKey).sourceRuleCount = getTotal(s.O, s.OID, s.Type);

                var o = list.Single(i => i.ID == m && i.Type == "O");
                var oKey = $"{o.Level}{o.O}{o.OID}";
                if (!model.nodes.Any(i => i.key == oKey))
                    model.nodes.Add(new JsonNodeItem { key = oKey, level = o.Level, obj = o.O, objid = o.OID, name = o.ObjectName, type = o.TypeName, objecttype = o.ObjectType, objecttypeid = o.ObjectTypeID, back = o.BackColor, fore = o.ForeColor, intersectMapId = o.ID, intersectId = o.IntersectID, sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type), mappingRuleCount = o.RawMappingRuleCount, challengeCount = o.ChallengeCount });
                else
                    model.nodes.First(i => i.key == oKey).sourceRuleCount = getTotalSourceRules(o.O, o.OID, o.Type);

                if (model.links.Any(i => i.from == sKey && i.to == oKey))
                {
                    var existingLink = model.links.Single(i => i.from == sKey && i.to == oKey);
                    existingLink.text += $", {s.Predicate}";
                }
                else
                {
                    model.links.Add(new JsonLinkItem { id = s.ID, intersectTypeId = s.IntersectTypeID, from = sKey, to = oKey, text = s.Predicate, predicateId = s.PredicateID, mappingRuleCount = s.LinkMappingRuleCount });
                }
            });

            model = MergeDiagram(model);

            return new JsonNetResult
            {
                Data = new { model.nodes, model.links },
                Formatting = Newtonsoft.Json.Formatting.None
            };

            //return new JsonNetResult { Data = null, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet]
        public JsonNetResult ChildRelationshipsBySourceAndTarget(SystemObjects s, int sID, SystemObjects t, int tID)
        {
            var sType = s.ToString();
            var tType = t.ToString();
            var sql = "";
            if (sType == tType && sID == tID)
            {
                //These objects are the same
                sql = @"select	R.* 
from	Relationship R
		inner join Relationship S on R.SourceObjectType = 'Intersect' 
										and S.IntersectID = R.SourceObjectID 
										and S.SourceObjectType = @sType 
										and S.SourceObjectID = @sID";
                return new JsonNetResult { Data = Company.Query<Relationship>(sql, new { sType = new Dapper.DbString { Value = sType, IsAnsi = true }, sID }).OrderBy(i => i.TargetTypeName).ThenBy(i => i.TargetName), Formatting = Newtonsoft.Json.Formatting.None };
            }
            else
            {
                //Objects are different
                sql = @"select	R.*
from	Relationship R
		inner join Relationship S on R.SourceObjectType = 'Intersect' 
										and S.IntersectID = R.SourceObjectID 
										and S.SourceObjectType = @sType 
										and S.SourceObjectID = @sID
										and S.TargetObjectType = @tType 
										and S.TargetObjectID = @tID";
                return new JsonNetResult { Data = Company.Query<Relationship>(sql, new { sType = new Dapper.DbString { Value = sType, IsAnsi = true }, sID, tType = new Dapper.DbString { Value = tType.ToString(), IsAnsi = true }, tID }).OrderBy(i => i.TargetTypeName).ThenBy(i => i.TargetName), Formatting = Newtonsoft.Json.Formatting.None };
            }
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
		from	    [utility].[RelationshipTypes] I
				    inner join cache.ObjectDetails ID on ID.[Object] = I.TargetObjectType and ID.ObjectID = I.TargetObjectID
		where	    SourceObjectType = 'IntersectType' and SourceObjectID = O.IntersectTypeID
        order by    ID.TextPath
		for         xml path('relationships'), TYPE
		),
		(
		select	    P.*
		from	    IntersectTypePredicate IP
				    inner join Predicate P on P.ID = IP.PredicateID and IP.IntersectTypeID = O.IntersectTypeID
		order by    P.Name
        for         xml path('predicates'), TYPE
		)
from	    [utility].[RelationshipTypes] O
		    inner join cache.ObjectDetails OD on OD.[Object] = O.TargetObjectType and OD.ObjectID = O.TargetObjectID 
where	    SourceObjectType = @type and SourceObjectID = @id
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

        public ActionResult AddRelationship(SystemObjects source, int sourceID, SystemObjects target, int targetID)
        {
            ViewData.Add("Source", source.ToString());
            ViewData.Add("SourceID", sourceID);
            ViewData.Add("TargetType", target.ToString());
            ViewData.Add("TargetTypeID", targetID);

            //var row = 0;
            //var list = new List<EditableField>();
            //list = loadDynamicFields(list, Company.GetFieldTypeRelationsByObject(SystemObjects.IntersectType, at).ToList(), row + 1);
            return PartialView();// (list);
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
                            var predicate = Company.GetById<Predicate>(model.PredicateID);

                            if (predicate.Type == MapType.Lineage)
                            {
                                if ($"{model.Target}{model.TargetID}" != $"{model.Subject}{model.SubjectID}")
                                    Company.AddRelationship(model.Target, model.TargetID, model.Subject, model.SubjectID, IntersectClassification.Normal, null, null);

                                if ($"{model.Target}{model.TargetID}" != $"{model.Object}{model.ObjectID}")
                                    Company.AddRelationship(model.Target, model.TargetID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
                            }


                            Company.AddRelationship(model.Subject, model.SubjectID, model.Object, model.ObjectID, IntersectClassification.Normal, null, null);
                            var intersect = Company.Query<IntersectLookupModel>(@"select top 1 
S.IntersectID,
S.ID as SubjectNodeID, S.[ObjectType] as Subject, S.ObjectID as SubjectID,
O.ID as ObjectNodeID, O.[ObjectType] as [Object], O.ObjectID 
from [IntersectNode] S 
inner join IntersectNode O on O.IntersectID = S.IntersectID 
and S.[ObjectType] = @s and S.ObjectID = @sid 
and O.[ObjectType] = @o and O.ObjectID = @oid",
        new { s = new Dapper.DbString { Value = model.Subject, IsAnsi = true }, sid = model.SubjectID, o = new Dapper.DbString { Value = model.Object, IsAnsi = true }, oid = model.ObjectID }
        ).SingleOrDefault();
                            if (intersect != null)
                            {
                                var existingSourceRecordCount = Company.Query<int>(
                                    "select count(1) from IntersectMap where SubjectIntersectNodeID = @s and ObjectIntersectNodeID = @o and PredicateID = @p", 
                                    new { s = intersect.SubjectNodeID, o = intersect.ObjectNodeID, p = model.PredicateID }
                                ).Single();

                                if (existingSourceRecordCount <= 0)
                                {
                                    // If we got here, we are all good.

                                    var intersectMap = new IntersectMap
                                    {
                                        ObjectIntersectNodeID = intersect.ObjectNodeID,// objectIntersectNode.ID,
                                        PredicateID = model.PredicateID,
                                        SubjectIntersectNodeID = intersect.SubjectNodeID,// subjectIntersectNode.ID,
                                        Type = MapType.Lineage
                                    };
                                    Company.Add<IntersectMap>(intersectMap);
                                    success = true;
                                }
                                else
                                {
                                    success = true;
                                    //message = $"There is already an existing source with this role.";
                                }
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

        [HttpGet, Route("update/{intersectMapID:int}/{predicateID:int}")]
        public JsonNetResult EditRelationship(int intersectMapID, int predicateID)
        {
            var message = "";
            var success = false;
            if (intersectMapID <= 0)
            {
                message = $"The intersect map ID ({intersectMapID}) is invalid.";
            }
            else
            {
                var record = Company.GetById<IntersectMap>(intersectMapID);
                if (record == null)
                {
                    message = $"The intersect map record with ID ({intersectMapID}) cound not be found.";
                }
                else
                {
                    record.PredicateID = predicateID;
                    Company.Update(record);
                    success = true;
                }
            }

            return new JsonNetResult { Data = new { message = message, success = success }, Formatting = Newtonsoft.Json.Formatting.None };
        }

        public ActionResult EditRelationship(int id)
        {
            ViewData.Add("IntersectID", id);
            var intersect = Company.GetById<Intersect>(id);
            var model = new EditRelationshipModel {
                Classification = intersect.Classification ?? 0,
                Description = intersect.Description,
                IntersectTypeID = intersect.IntersectTypeID
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
            ViewData.Add("source", source.Name);
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
            ViewData.Add("Object", type.ToString());
            ViewData.Add("ObjectID", id.ToString());
            var model = Company.GetObjectDetail(type, id);
            return PartialView(model);
        }

        #endregion
    }
}
